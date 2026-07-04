using Helpdesk.Domain.Assets;
using Helpdesk.Domain.Audit;
using Helpdesk.Domain.Tickets;
using Helpdesk.Domain.Users;
using Helpdesk.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(HelpdeskDbContext db, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        if (!await db.Categories.AnyAsync(cancellationToken))
        {
            db.Categories.AddRange(
                new TicketCategory("Hardware", "Devices, peripherals and physical equipment."),
                new TicketCategory("Software", "Installed applications, licensing and updates."),
                new TicketCategory("Network", "Connectivity, VPN, Wi-Fi and internal services."),
                new TicketCategory("Account/Access", "Passwords, permissions and onboarding access."),
                new TicketCategory("Printer", "Printers, scanners and print queues."),
                new TicketCategory("Email", "Mailboxes, spam, delivery and calendars."),
                new TicketCategory("Other", "Anything that does not fit another category."));
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.TicketTags.AnyAsync(cancellationToken))
        {
            db.TicketTags.AddRange(
                new TicketTag("VIP", "#8B5CF6", now),
                new TicketTag("Hardware", "#2563EB", now),
                new TicketTag("Access", "#059669", now),
                new TicketTag("Escalation", "#DC2626", now),
                new TicketTag("Waiting vendor", "#D97706", now));
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            db.Users.AddRange(
                new AppUser("Ewa Nowak", "employee@test.com", PasswordHasher.Hash("Password123!"), UserRole.Employee, "Finance"),
                new AppUser("Tomasz Zielinski", "technician@test.com", PasswordHasher.Hash("Password123!"), UserRole.Technician, "IT Support"),
                new AppUser("Marta Lewandowska", "admin@test.com", PasswordHasher.Hash("Password123!"), UserRole.Admin, "IT Management"));
            await db.SaveChangesAsync(cancellationToken);
        }

        var employee = await db.Users.SingleAsync(user => user.Email == "employee@test.com", cancellationToken);
        var technician = await db.Users.SingleAsync(user => user.Email == "technician@test.com", cancellationToken);
        var admin = await db.Users.SingleAsync(user => user.Email == "admin@test.com", cancellationToken);

        if (!await db.Assets.AnyAsync(cancellationToken))
        {
            var laptop = new Asset(
                "LTP-2026-001",
                AssetType.Laptop,
                "Lenovo",
                "ThinkPad T14 Gen 6",
                "SN-T14-2026-001",
                DateOnly.FromDateTime(now.AddMonths(-5).Date),
                DateOnly.FromDateTime(now.AddYears(3).Date),
                "Primary device assigned to Finance.");
            laptop.AssignTo(employee.Id, admin.Id, now.AddMonths(-4));

            db.Assets.AddRange(
                laptop,
                new Asset("MON-2026-014", AssetType.Monitor, "Dell", "UltraSharp U2725", "SN-U2725-014", DateOnly.FromDateTime(now.AddMonths(-2).Date), DateOnly.FromDateTime(now.AddYears(3).Date), "Spare monitor."),
                new Asset("NET-2025-002", AssetType.Router, "Ubiquiti", "Dream Router", "SN-UDR-002", DateOnly.FromDateTime(now.AddYears(-1).Date), DateOnly.FromDateTime(now.AddYears(1).Date), "Branch office router."),
                new Asset("PRN-2024-006", AssetType.Printer, "Brother", "MFC-L8900CDW", "SN-BR-006", DateOnly.FromDateTime(now.AddYears(-2).Date), DateOnly.FromDateTime(now.AddMonths(8).Date), "Shared office printer."));
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Tickets.AnyAsync(cancellationToken))
        {
            var hardware = await db.Categories.SingleAsync(category => category.Name == "Hardware", cancellationToken);
            var access = await db.Categories.SingleAsync(category => category.Name == "Account/Access", cancellationToken);
            var network = await db.Categories.SingleAsync(category => category.Name == "Network", cancellationToken);
            var laptop = await db.Assets.SingleAsync(asset => asset.AssetTag == "LTP-2026-001", cancellationToken);
            var hardwareTag = await db.TicketTags.SingleAsync(tag => tag.Name == "Hardware", cancellationToken);
            var accessTag = await db.TicketTags.SingleAsync(tag => tag.Name == "Access", cancellationToken);
            var escalationTag = await db.TicketTags.SingleAsync(tag => tag.Name == "Escalation", cancellationToken);

            var overheating = Ticket.Create(
                "Laptop overheats during video calls",
                "Device becomes very hot after a few minutes in Teams meetings and fans run constantly.",
                hardware.Id,
                TicketPriority.High,
                employee.Id,
                now.AddHours(-8));
            overheating.AssignTo(technician.Id, now.AddHours(-7));
            overheating.ChangeStatus(TicketStatus.InProgress, now.AddHours(-6));
            overheating.LinkAsset(laptop.Id, now.AddHours(-6));
            overheating.AddComment(new TicketComment(overheating.Id, employee.Id, "It happens mostly when I share the screen.", false, now.AddHours(-5)), now.AddHours(-5));
            overheating.AddComment(new TicketComment(overheating.Id, technician.Id, "Thermal diagnostics started. Checking firmware and fan curve.", true, now.AddHours(-4)), now.AddHours(-4));

            var vpn = Ticket.Create(
                "VPN access missing after role change",
                "I moved to a new project and cannot connect to the client VPN profile.",
                access.Id,
                TicketPriority.Medium,
                employee.Id,
                now.AddDays(-2));
            vpn.AssignTo(technician.Id, now.AddDays(-2).AddHours(1));
            vpn.ChangeStatus(TicketStatus.Resolved, now.AddDays(-1).AddHours(3));

            var wifi = Ticket.Create(
                "Meeting room Wi-Fi drops calls",
                "Several users reported unstable connection in room B-204.",
                network.Id,
                TicketPriority.Critical,
                admin.Id,
                now.AddHours(-6));

            db.Tickets.AddRange(overheating, vpn, wifi);
            db.TicketTagAssignments.AddRange(
                new TicketTagAssignment(overheating.Id, hardwareTag.Id, now.AddHours(-8)),
                new TicketTagAssignment(vpn.Id, accessTag.Id, now.AddDays(-2)),
                new TicketTagAssignment(wifi.Id, escalationTag.Id, now.AddHours(-6)));
            db.AuditLogs.AddRange(
                TicketAudit(overheating.Id, "Ticket created", null, "High hardware issue", employee.Id, now.AddHours(-8)),
                TicketAudit(overheating.Id, "Assigned technician changed", null, technician.FullName, technician.Id, now.AddHours(-7)),
                TicketAudit(overheating.Id, "Status changed", TicketStatus.Assigned.ToString(), TicketStatus.InProgress.ToString(), technician.Id, now.AddHours(-6)),
                TicketAudit(overheating.Id, "Asset linked to ticket", null, laptop.AssetTag, technician.Id, now.AddHours(-6)),
                TicketAudit(overheating.Id, "Comment added", null, "Public comment", employee.Id, now.AddHours(-5)),
                TicketAudit(overheating.Id, "Comment added", null, "Internal comment", technician.Id, now.AddHours(-4)),
                TicketAudit(vpn.Id, "Ticket created", null, "Medium access issue", employee.Id, now.AddDays(-2)),
                TicketAudit(vpn.Id, "Status changed", TicketStatus.Assigned.ToString(), TicketStatus.Resolved.ToString(), technician.Id, now.AddDays(-1).AddHours(3)),
                TicketAudit(wifi.Id, "Ticket created", null, "Critical network issue", admin.Id, now.AddHours(-6)));

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static AuditLog TicketAudit(Guid ticketId, string action, string? oldValue, string? newValue, Guid userId, DateTimeOffset createdAt)
    {
        return new AuditLog("Ticket", ticketId, action, oldValue, newValue, userId, createdAt);
    }
}
