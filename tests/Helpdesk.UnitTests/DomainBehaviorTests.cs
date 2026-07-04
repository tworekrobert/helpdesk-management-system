using System.Text;
using Helpdesk.Application.Reports;
using Helpdesk.Domain.Assets;
using Helpdesk.Domain.Common;
using Helpdesk.Domain.Notifications;
using Helpdesk.Domain.Tickets;

namespace Helpdesk.UnitTests;

public sealed class DomainBehaviorTests
{
    private static readonly Guid CategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EmployeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TechnicianId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid OtherUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void LowPrioritySlaReturnsCreatedAtPlus72Hours()
    {
        var dueAt = SlaPolicy.CalculateDueAt(CreatedAt, TicketPriority.Low);

        Assert.Equal(CreatedAt.AddHours(72), dueAt);
    }

    [Fact]
    public void CriticalPrioritySlaReturnsCreatedAtPlus4Hours()
    {
        var dueAt = SlaPolicy.CalculateDueAt(CreatedAt, TicketPriority.Critical);

        Assert.Equal(CreatedAt.AddHours(4), dueAt);
    }

    [Fact]
    public void AssigningTicketMovesItFromNewToAssigned()
    {
        var ticket = CreateTicket();

        ticket.AssignTo(TechnicianId, CreatedAt.AddMinutes(10));

        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        Assert.Equal(TechnicianId, ticket.AssignedTechnicianId);
    }

    [Fact]
    public void TicketCanMoveFromAssignedToInProgress()
    {
        var ticket = CreateTicket();
        ticket.AssignTo(TechnicianId, CreatedAt.AddMinutes(10));

        ticket.ChangeStatus(TicketStatus.InProgress, CreatedAt.AddMinutes(20));

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
    }

    [Fact]
    public void TicketCannotMoveFromClosedToInProgress()
    {
        var ticket = CreateTicket();
        ticket.AssignTo(TechnicianId, CreatedAt.AddMinutes(10));
        ticket.ChangeStatus(TicketStatus.InProgress, CreatedAt.AddMinutes(20));
        ticket.ChangeStatus(TicketStatus.Resolved, CreatedAt.AddMinutes(30));
        ticket.ChangeStatus(TicketStatus.Closed, CreatedAt.AddMinutes(40));

        var exception = Assert.Throws<DomainException>(() =>
            ticket.ChangeStatus(TicketStatus.InProgress, CreatedAt.AddMinutes(50)));

        Assert.Contains("cannot move", exception.Message);
    }

    [Fact]
    public void ResolvedAtIsSetWhenStatusChangesToResolved()
    {
        var ticket = CreateTicket();
        var resolvedAt = CreatedAt.AddHours(2);
        ticket.AssignTo(TechnicianId, CreatedAt.AddMinutes(10));
        ticket.ChangeStatus(TicketStatus.InProgress, CreatedAt.AddMinutes(20));

        ticket.ChangeStatus(TicketStatus.Resolved, resolvedAt);

        Assert.Equal(resolvedAt, ticket.ResolvedAt);
    }

    [Fact]
    public void OverdueFlagIsFalseForResolvedAndClosedTickets()
    {
        var ticket = CreateTicket(TicketPriority.Critical);
        ticket.AssignTo(TechnicianId, CreatedAt.AddMinutes(10));
        ticket.ChangeStatus(TicketStatus.InProgress, CreatedAt.AddMinutes(20));
        ticket.ChangeStatus(TicketStatus.Resolved, CreatedAt.AddHours(1));

        Assert.False(ticket.IsOverdue(CreatedAt.AddDays(1)));

        ticket.ChangeStatus(TicketStatus.Closed, CreatedAt.AddHours(2));

        Assert.False(ticket.IsOverdue(CreatedAt.AddDays(2)));
    }

    [Fact]
    public void AssetCannotBeAssignedToTwoUsersAtTheSameTime()
    {
        var asset = new Asset(
            "LTP-001",
            AssetType.Laptop,
            "Lenovo",
            "ThinkPad",
            "SN-001",
            DateOnly.FromDateTime(CreatedAt.Date),
            DateOnly.FromDateTime(CreatedAt.AddYears(3).Date),
            null);
        asset.AssignTo(EmployeeId, TechnicianId, CreatedAt);

        var exception = Assert.Throws<DomainException>(() =>
            asset.AssignTo(OtherUserId, TechnicianId, CreatedAt.AddMinutes(5)));

        Assert.Contains("already assigned", exception.Message);
    }

    [Fact]
    public void ReturningAssetClosesActiveAssignmentAtProvidedTime()
    {
        var asset = new Asset(
            "LTP-002",
            AssetType.Laptop,
            "Lenovo",
            "ThinkPad",
            "SN-002",
            DateOnly.FromDateTime(CreatedAt.Date),
            DateOnly.FromDateTime(CreatedAt.AddYears(3).Date),
            null);
        var returnedAt = CreatedAt.AddDays(1);
        asset.AssignTo(EmployeeId, TechnicianId, CreatedAt);

        asset.ChangeStatus(AssetStatus.Available, returnedAt);

        Assert.Null(asset.AssignedToUserId);
        Assert.Equal(AssetStatus.Available, asset.Status);
        Assert.Contains(asset.Assignments, assignment =>
            assignment.UserId == EmployeeId
            && assignment.ReturnedAt == returnedAt);
    }

    [Fact]
    public void ClosedTicketCannotBeCommented()
    {
        var ticket = CreateTicket();
        ticket.AssignTo(TechnicianId, CreatedAt.AddMinutes(10));
        ticket.ChangeStatus(TicketStatus.InProgress, CreatedAt.AddMinutes(20));
        ticket.ChangeStatus(TicketStatus.Resolved, CreatedAt.AddMinutes(30));
        ticket.ChangeStatus(TicketStatus.Closed, CreatedAt.AddMinutes(40));
        var comment = new TicketComment(ticket.Id, EmployeeId, "Can you reopen this?", false, CreatedAt.AddMinutes(45));

        var exception = Assert.Throws<DomainException>(() =>
            ticket.AddComment(comment, CreatedAt.AddMinutes(45)));

        Assert.Contains("cannot be commented", exception.Message);
    }

    [Fact]
    public void ChangingPriorityRecalculatesSlaFromOriginalCreationTime()
    {
        var ticket = CreateTicket(TicketPriority.Low);

        ticket.ChangePriority(TicketPriority.Critical, CreatedAt.AddHours(3));

        Assert.Equal(CreatedAt.AddHours(4), ticket.SlaDueAt);
    }

    [Fact]
    public void PdfReportCreatesValidPdfDocument()
    {
        var pdf = SimplePdfReport.Create("Helpdesk report", ["Open tickets: 3"]);
        var text = Encoding.ASCII.GetString(pdf);

        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("Helpdesk report", text);
        Assert.Contains("Open tickets: 3", text);
        Assert.Contains("xref", text);
    }

    [Fact]
    public void TicketTagStoresTrimmedDisplayValues()
    {
        var tag = new TicketTag("  Escalation  ", "  #DC2626  ", CreatedAt);

        Assert.Equal("Escalation", tag.Name);
        Assert.Equal("#DC2626", tag.Color);
        Assert.True(tag.IsActive);
    }

    [Fact]
    public void EmailNotificationCanBeMarkedFailed()
    {
        var notification = new EmailNotification(
            EmployeeId,
            "employee@test.com",
            "Ticket status changed",
            "Ticket HD-00001 was resolved.",
            CreatedAt);
        var processedAt = CreatedAt.AddMinutes(1);

        notification.MarkFailed("SMTP unavailable", processedAt);

        Assert.Equal(EmailNotificationStatus.Failed, notification.Status);
        Assert.Equal(processedAt, notification.ProcessedAt);
        Assert.Equal("SMTP unavailable", notification.Error);
    }

    private static Ticket CreateTicket(TicketPriority priority = TicketPriority.Medium)
    {
        return Ticket.Create(
            "Cannot access VPN",
            "VPN client reports invalid profile after onboarding.",
            CategoryId,
            priority,
            EmployeeId,
            CreatedAt);
    }
}
