using Helpdesk.Domain.Assets;
using Helpdesk.Domain.Audit;
using Helpdesk.Domain.Notifications;
using Helpdesk.Domain.Tickets;
using Helpdesk.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Infrastructure.Data;

public sealed class HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketCategory> Categories => Set<TicketCategory>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketTag> TicketTags => Set<TicketTag>();
    public DbSet<TicketTagAssignment> TicketTagAssignments => Set<TicketTagAssignment>();
    public DbSet<EmailNotification> EmailNotifications => Set<EmailNotification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetAssignment> AssetAssignments => Set<AssetAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).ValueGeneratedNever();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.FullName).HasMaxLength(160).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(user => user.Department).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<TicketCategory>(entity =>
        {
            entity.ToTable("ticket_categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).ValueGeneratedNever();
            entity.HasIndex(category => category.Name).IsUnique();
            entity.Property(category => category.Name).HasMaxLength(80).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(240).IsRequired();
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");
            entity.HasKey(ticket => ticket.Id);
            entity.Property(ticket => ticket.Id).ValueGeneratedNever();
            entity.HasIndex(ticket => ticket.Number).IsUnique();
            entity.Property(ticket => ticket.Number).ValueGeneratedOnAdd();
            entity.Property(ticket => ticket.Title).HasMaxLength(180).IsRequired();
            entity.Property(ticket => ticket.Description).HasMaxLength(4000).IsRequired();
            entity.Property(ticket => ticket.Priority).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(ticket => ticket.Status).HasConversion<string>().HasMaxLength(40).IsRequired();

            entity.HasOne(ticket => ticket.Category)
                .WithMany()
                .HasForeignKey(ticket => ticket.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(ticket => ticket.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(ticket => ticket.AssignedTechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Asset>()
                .WithMany()
                .HasForeignKey(ticket => ticket.RelatedAssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(ticket => ticket.Comments)
                .WithOne()
                .HasForeignKey(comment => comment.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TicketComment>(entity =>
        {
            entity.ToTable("ticket_comments");
            entity.HasKey(comment => comment.Id);
            entity.Property(comment => comment.Id).ValueGeneratedNever();
            entity.Property(comment => comment.Body).HasMaxLength(3000).IsRequired();
            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(comment => comment.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketAttachment>(entity =>
        {
            entity.ToTable("ticket_attachments");
            entity.HasKey(attachment => attachment.Id);
            entity.Property(attachment => attachment.Id).ValueGeneratedNever();
            entity.Property(attachment => attachment.FileName).HasMaxLength(240).IsRequired();
            entity.Property(attachment => attachment.ContentType).HasMaxLength(120).IsRequired();
            entity.Property(attachment => attachment.Content).IsRequired();
            entity.HasIndex(attachment => new { attachment.TicketId, attachment.CreatedAt });
            entity.HasOne<Ticket>()
                .WithMany()
                .HasForeignKey(attachment => attachment.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(attachment => attachment.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketTag>(entity =>
        {
            entity.ToTable("ticket_tags");
            entity.HasKey(tag => tag.Id);
            entity.Property(tag => tag.Id).ValueGeneratedNever();
            entity.HasIndex(tag => tag.Name).IsUnique();
            entity.Property(tag => tag.Name).HasMaxLength(80).IsRequired();
            entity.Property(tag => tag.Color).HasMaxLength(24).IsRequired();
        });

        modelBuilder.Entity<TicketTagAssignment>(entity =>
        {
            entity.ToTable("ticket_tag_assignments");
            entity.HasKey(assignment => new { assignment.TicketId, assignment.TagId });
            entity.HasIndex(assignment => assignment.TagId);
            entity.HasOne(assignment => assignment.Ticket)
                .WithMany()
                .HasForeignKey(assignment => assignment.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(assignment => assignment.Tag)
                .WithMany(tag => tag.Assignments)
                .HasForeignKey(assignment => assignment.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailNotification>(entity =>
        {
            entity.ToTable("email_notifications");
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Id).ValueGeneratedNever();
            entity.HasIndex(notification => new { notification.Status, notification.CreatedAt });
            entity.Property(notification => notification.ToEmail).HasMaxLength(256).IsRequired();
            entity.Property(notification => notification.Subject).HasMaxLength(240).IsRequired();
            entity.Property(notification => notification.Body).HasMaxLength(4000).IsRequired();
            entity.Property(notification => notification.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(notification => notification.Error).HasMaxLength(1000);
            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(notification => notification.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Id).ValueGeneratedNever();
            entity.HasIndex(log => new { log.EntityType, log.EntityId, log.CreatedAt });
            entity.Property(log => log.EntityType).HasMaxLength(80).IsRequired();
            entity.Property(log => log.Action).HasMaxLength(160).IsRequired();
            entity.Property(log => log.OldValue).HasMaxLength(1000);
            entity.Property(log => log.NewValue).HasMaxLength(1000);
            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.ToTable("assets");
            entity.HasKey(asset => asset.Id);
            entity.Property(asset => asset.Id).ValueGeneratedNever();
            entity.HasIndex(asset => asset.AssetTag).IsUnique();
            entity.HasIndex(asset => asset.SerialNumber).IsUnique();
            entity.Property(asset => asset.AssetTag).HasMaxLength(80).IsRequired();
            entity.Property(asset => asset.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(asset => asset.Brand).HasMaxLength(120).IsRequired();
            entity.Property(asset => asset.Model).HasMaxLength(160).IsRequired();
            entity.Property(asset => asset.SerialNumber).HasMaxLength(160).IsRequired();
            entity.Property(asset => asset.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(asset => asset.Notes).HasMaxLength(1000);

            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(asset => asset.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(asset => asset.Assignments)
                .WithOne()
                .HasForeignKey(assignment => assignment.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssetAssignment>(entity =>
        {
            entity.ToTable("asset_assignments");
            entity.HasKey(assignment => assignment.Id);
            entity.Property(assignment => assignment.Id).ValueGeneratedNever();
            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(assignment => assignment.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(assignment => assignment.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
