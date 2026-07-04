using Helpdesk.Domain.Common;

namespace Helpdesk.Domain.Assets;

public sealed class Asset
{
    private Asset()
    {
    }

    public Asset(
        string assetTag,
        AssetType type,
        string brand,
        string model,
        string serialNumber,
        DateOnly purchaseDate,
        DateOnly warrantyUntil,
        string? notes)
    {
        Id = Guid.NewGuid();
        AssetTag = assetTag.Trim().ToUpperInvariant();
        Type = type;
        Brand = brand.Trim();
        Model = model.Trim();
        SerialNumber = serialNumber.Trim();
        Status = AssetStatus.Available;
        PurchaseDate = purchaseDate;
        WarrantyUntil = warrantyUntil;
        Notes = notes?.Trim();
    }

    public Guid Id { get; private set; }
    public string AssetTag { get; private set; } = string.Empty;
    public AssetType Type { get; private set; }
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string SerialNumber { get; private set; } = string.Empty;
    public AssetStatus Status { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public DateOnly PurchaseDate { get; private set; }
    public DateOnly WarrantyUntil { get; private set; }
    public string? Notes { get; private set; }
    public List<AssetAssignment> Assignments { get; private set; } = [];

    public void UpdateDetails(
        AssetType type,
        string brand,
        string model,
        string serialNumber,
        DateOnly purchaseDate,
        DateOnly warrantyUntil,
        string? notes)
    {
        Type = type;
        Brand = brand.Trim();
        Model = model.Trim();
        SerialNumber = serialNumber.Trim();
        PurchaseDate = purchaseDate;
        WarrantyUntil = warrantyUntil;
        Notes = notes?.Trim();
    }

    public void AssignTo(Guid userId, Guid assignedByUserId, DateTimeOffset now)
    {
        if (AssignedToUserId.HasValue && AssignedToUserId.Value != userId)
        {
            throw new DomainException("Asset is already assigned to another user.");
        }

        AssignedToUserId = userId;
        Status = AssetStatus.Assigned;
        Assignments.Add(new AssetAssignment(Id, userId, assignedByUserId, now));
    }

    public void ChangeStatus(AssetStatus status, DateTimeOffset now)
    {
        if (status == AssetStatus.Assigned && AssignedToUserId is null)
        {
            throw new DomainException("Asset cannot be marked as assigned without a user.");
        }

        if (status == AssetStatus.Available)
        {
            AssignedToUserId = null;
            foreach (var assignment in Assignments.Where(assignment => assignment.ReturnedAt is null))
            {
                assignment.MarkReturned(now);
            }
        }

        Status = status;
    }
}
