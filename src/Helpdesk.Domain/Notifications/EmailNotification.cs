namespace Helpdesk.Domain.Notifications;

public sealed class EmailNotification
{
    private EmailNotification()
    {
    }

    public EmailNotification(
        Guid recipientUserId,
        string toEmail,
        string subject,
        string body,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        RecipientUserId = recipientUserId;
        ToEmail = toEmail.Trim();
        Subject = subject.Trim();
        Body = body.Trim();
        Status = EmailNotificationStatus.Pending;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string ToEmail { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public EmailNotificationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

    public void MarkSent(DateTimeOffset processedAt)
    {
        Status = EmailNotificationStatus.Sent;
        ProcessedAt = processedAt;
        Error = null;
    }

    public void MarkFailed(string error, DateTimeOffset processedAt)
    {
        Status = EmailNotificationStatus.Failed;
        ProcessedAt = processedAt;
        Error = error.Trim();
    }
}
