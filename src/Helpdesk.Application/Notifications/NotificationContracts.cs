using Helpdesk.Domain.Notifications;

namespace Helpdesk.Application.Notifications;

public sealed record EmailNotificationResponse(
    Guid Id,
    Guid RecipientUserId,
    string ToEmail,
    string Subject,
    string Body,
    EmailNotificationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    string? Error);
