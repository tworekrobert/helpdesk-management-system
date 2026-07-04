using Helpdesk.Api.Extensions;
using Helpdesk.Application.Assets;
using Helpdesk.Domain.Assets;
using Helpdesk.Domain.Audit;
using Helpdesk.Domain.Common;
using Helpdesk.Domain.Users;
using Helpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/assets")]
public sealed class AssetsController(HelpdeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AssetResponse>>> List(
        [FromQuery] AssetStatus? status,
        [FromQuery] AssetType? type,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();

        var query = db.Assets.AsNoTracking().AsQueryable();
        if (role == UserRole.Employee)
        {
            query = query.Where(asset => asset.AssignedToUserId == currentUserId);
        }

        if (status is not null)
        {
            query = query.Where(asset => asset.Status == status);
        }

        if (type is not null)
        {
            query = query.Where(asset => asset.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(asset =>
                asset.AssetTag.ToLower().Contains(normalized)
                || asset.Brand.ToLower().Contains(normalized)
                || asset.Model.ToLower().Contains(normalized)
                || asset.SerialNumber.ToLower().Contains(normalized));
        }

        var assets = await query.OrderBy(asset => asset.AssetTag).ToListAsync(cancellationToken);
        return Ok(await MapAssetsAsync(assets, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetDetailsResponse>> Details(Guid id, CancellationToken cancellationToken)
    {
        var asset = await db.Assets
            .AsNoTracking()
            .Include(candidate => candidate.Assignments)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (asset is null)
        {
            return NotFound();
        }

        if (User.GetUserRole() == UserRole.Employee && asset.AssignedToUserId != User.GetUserId())
        {
            return Forbid();
        }

        return Ok(await MapAssetDetailsAsync(asset, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AssetResponse>> Create(CreateAssetRequest request, CancellationToken cancellationToken)
    {
        var normalizedTag = request.AssetTag.Trim().ToUpperInvariant();
        var normalizedSerial = request.SerialNumber.Trim();
        var exists = await db.Assets.AnyAsync(asset => asset.AssetTag == normalizedTag || asset.SerialNumber == normalizedSerial, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "Asset tag or serial number already exists." });
        }

        var now = DateTimeOffset.UtcNow;
        var asset = new Asset(request.AssetTag, request.Type, request.Brand, request.Model, request.SerialNumber, request.PurchaseDate, request.WarrantyUntil, request.Notes);
        db.Assets.Add(asset);
        db.AuditLogs.Add(new AuditLog("Asset", asset.Id, "Asset created", null, asset.AssetTag, User.GetUserId(), now));
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Details), new { id = asset.Id }, (await MapAssetsAsync([asset], cancellationToken)).Single());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AssetResponse>> Update(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (asset is null)
        {
            return NotFound();
        }

        var normalizedSerial = request.SerialNumber.Trim();
        var serialExists = await db.Assets.AnyAsync(candidate => candidate.Id != id && candidate.SerialNumber == normalizedSerial, cancellationToken);
        if (serialExists)
        {
            return Conflict(new { message = "Asset serial number already exists." });
        }

        var oldValue = FormatAssetDetails(asset);
        var newValue = FormatAssetDetails(request.Type, request.Brand, request.Model, normalizedSerial);
        asset.UpdateDetails(request.Type, request.Brand, request.Model, request.SerialNumber, request.PurchaseDate, request.WarrantyUntil, request.Notes);
        db.AuditLogs.Add(new AuditLog("Asset", asset.Id, "Asset updated", oldValue, newValue, User.GetUserId(), DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(cancellationToken);
        return Ok((await MapAssetsAsync([asset], cancellationToken)).Single());
    }

    [HttpPatch("{id:guid}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AssetResponse>> Assign(Guid id, AssignAssetRequest request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets
            .Include(candidate => candidate.Assignments)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (asset is null)
        {
            return NotFound();
        }

        var targetUser = await db.Users.SingleOrDefaultAsync(user => user.Id == request.UserId && user.IsActive, cancellationToken);
        if (targetUser is null)
        {
            return BadRequest(new { message = "Target user does not exist or is inactive." });
        }

        var oldAssignee = asset.AssignedToUserId is null
            ? null
            : await db.Users
                .AsNoTracking()
                .Where(user => user.Id == asset.AssignedToUserId.Value)
                .Select(user => user.FullName)
                .SingleOrDefaultAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        try
        {
            asset.AssignTo(targetUser.Id, User.GetUserId(), now);
        }
        catch (DomainException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        db.AuditLogs.Add(new AuditLog("Asset", asset.Id, "Asset assigned", oldAssignee, targetUser.FullName, User.GetUserId(), now));
        await db.SaveChangesAsync(cancellationToken);
        return Ok((await MapAssetsAsync([asset], cancellationToken)).Single());
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Technician,Admin")]
    public async Task<ActionResult<AssetResponse>> ChangeStatus(Guid id, ChangeAssetStatusRequest request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets
            .Include(candidate => candidate.Assignments)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (asset is null)
        {
            return NotFound();
        }

        var oldStatus = asset.Status.ToString();
        var now = DateTimeOffset.UtcNow;
        try
        {
            asset.ChangeStatus(request.Status, now);
        }
        catch (DomainException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        var newStatus = asset.Status.ToString();
        if (oldStatus != newStatus)
        {
            db.AuditLogs.Add(new AuditLog("Asset", asset.Id, "Asset status changed", oldStatus, newStatus, User.GetUserId(), now));
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok((await MapAssetsAsync([asset], cancellationToken)).Single());
    }

    private async Task<IReadOnlyCollection<AssetResponse>> MapAssetsAsync(IReadOnlyCollection<Asset> assets, CancellationToken cancellationToken)
    {
        var userIds = assets
            .Where(asset => asset.AssignedToUserId.HasValue)
            .Select(asset => asset.AssignedToUserId!.Value)
            .Distinct()
            .ToArray();
        var users = await db.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        return assets.Select(asset => new AssetResponse(
            asset.Id,
            asset.AssetTag,
            asset.Type,
            asset.Brand,
            asset.Model,
            asset.SerialNumber,
            asset.Status,
            asset.AssignedToUserId,
            asset.AssignedToUserId is null ? null : users[asset.AssignedToUserId.Value].FullName,
            asset.PurchaseDate,
            asset.WarrantyUntil,
            asset.Notes)).ToList();
    }

    private async Task<AssetDetailsResponse> MapAssetDetailsAsync(Asset asset, CancellationToken cancellationToken)
    {
        var userIds = asset.Assignments
            .SelectMany(assignment => new[] { assignment.UserId, assignment.AssignedByUserId })
            .Concat(asset.AssignedToUserId.HasValue ? [asset.AssignedToUserId.Value] : [])
            .Distinct()
            .ToArray();
        var users = await db.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        return new AssetDetailsResponse(
            asset.Id,
            asset.AssetTag,
            asset.Type,
            asset.Brand,
            asset.Model,
            asset.SerialNumber,
            asset.Status,
            asset.AssignedToUserId,
            asset.AssignedToUserId is null ? null : users[asset.AssignedToUserId.Value].FullName,
            asset.PurchaseDate,
            asset.WarrantyUntil,
            asset.Notes,
            asset.Assignments
                .OrderByDescending(assignment => assignment.AssignedAt)
                .Select(assignment => new AssetAssignmentResponse(
                    assignment.Id,
                    assignment.UserId,
                    users[assignment.UserId].FullName,
                    assignment.AssignedByUserId,
                    users[assignment.AssignedByUserId].FullName,
                    assignment.AssignedAt,
                    assignment.ReturnedAt))
                .ToList());
    }

    private static string FormatAssetDetails(Asset asset)
    {
        return FormatAssetDetails(asset.Type, asset.Brand, asset.Model, asset.SerialNumber);
    }

    private static string FormatAssetDetails(AssetType type, string brand, string model, string serialNumber)
    {
        return $"{type}: {brand.Trim()} {model.Trim()} ({serialNumber.Trim()})";
    }
}
