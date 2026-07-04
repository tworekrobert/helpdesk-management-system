namespace Helpdesk.Domain.Assets;

public sealed class AssetAssignment
{
    private AssetAssignment()
    {
    }

    public AssetAssignment(Guid assetId, Guid userId, Guid assignedByUserId, DateTimeOffset assignedAt)
    {
        Id = Guid.NewGuid();
        AssetId = assetId;
        UserId = userId;
        AssignedByUserId = assignedByUserId;
        AssignedAt = assignedAt;
    }

    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AssignedByUserId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? ReturnedAt { get; private set; }

    public void MarkReturned(DateTimeOffset returnedAt)
    {
        ReturnedAt = returnedAt;
    }
}
