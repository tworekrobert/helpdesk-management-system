namespace Helpdesk.Domain.Tickets;

public sealed class TicketCategory
{
    private TicketCategory()
    {
    }

    public TicketCategory(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description.Trim();
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public void Update(string name, string description, bool isActive)
    {
        Name = name.Trim();
        Description = description.Trim();
        IsActive = isActive;
    }
}
