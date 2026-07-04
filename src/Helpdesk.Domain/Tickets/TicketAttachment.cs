namespace Helpdesk.Domain.Tickets;

public sealed class TicketAttachment
{
    private TicketAttachment()
    {
    }

    public TicketAttachment(
        Guid ticketId,
        Guid uploadedByUserId,
        string fileName,
        string contentType,
        long size,
        byte[] content,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        TicketId = ticketId;
        UploadedByUserId = uploadedByUserId;
        FileName = fileName.Trim();
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        Size = size;
        Content = content;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public byte[] Content { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }
}
