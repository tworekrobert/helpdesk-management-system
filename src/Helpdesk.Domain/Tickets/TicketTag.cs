using Helpdesk.Domain.Common;

namespace Helpdesk.Domain.Tickets;

public sealed class TicketTag
{
    private TicketTag()
    {
    }

    public TicketTag(string name, string color, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Tag name is required.");
        }

        if (string.IsNullOrWhiteSpace(color))
        {
            throw new DomainException("Tag color is required.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Color = color.Trim();
        IsActive = true;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public List<TicketTagAssignment> Assignments { get; private set; } = [];

    public void Update(string name, string color, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Tag name is required.");
        }

        if (string.IsNullOrWhiteSpace(color))
        {
            throw new DomainException("Tag color is required.");
        }

        Name = name.Trim();
        Color = color.Trim();
        IsActive = isActive;
    }
}
