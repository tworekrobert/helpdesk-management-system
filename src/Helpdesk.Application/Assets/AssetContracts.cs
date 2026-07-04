using Helpdesk.Domain.Assets;

namespace Helpdesk.Application.Assets;

public sealed record CreateAssetRequest(
    string AssetTag,
    AssetType Type,
    string Brand,
    string Model,
    string SerialNumber,
    DateOnly PurchaseDate,
    DateOnly WarrantyUntil,
    string? Notes);

public sealed record UpdateAssetRequest(
    AssetType Type,
    string Brand,
    string Model,
    string SerialNumber,
    DateOnly PurchaseDate,
    DateOnly WarrantyUntil,
    string? Notes);

public sealed record AssignAssetRequest(Guid UserId);

public sealed record ChangeAssetStatusRequest(AssetStatus Status);

public sealed record AssetResponse(
    Guid Id,
    string AssetTag,
    AssetType Type,
    string Brand,
    string Model,
    string SerialNumber,
    AssetStatus Status,
    Guid? AssignedToUserId,
    string? AssignedTo,
    DateOnly PurchaseDate,
    DateOnly WarrantyUntil,
    string? Notes);

public sealed record AssetDetailsResponse(
    Guid Id,
    string AssetTag,
    AssetType Type,
    string Brand,
    string Model,
    string SerialNumber,
    AssetStatus Status,
    Guid? AssignedToUserId,
    string? AssignedTo,
    DateOnly PurchaseDate,
    DateOnly WarrantyUntil,
    string? Notes,
    IReadOnlyCollection<AssetAssignmentResponse> AssignmentHistory);

public sealed record AssetAssignmentResponse(
    Guid Id,
    Guid UserId,
    string User,
    Guid AssignedByUserId,
    string AssignedBy,
    DateTimeOffset AssignedAt,
    DateTimeOffset? ReturnedAt);
