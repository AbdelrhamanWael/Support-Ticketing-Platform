using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketingPlatform.Domain.Entities;

namespace SupportTicketingPlatform.Infrastructure.Persistence.Configurations
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            // Indexes for fast querying (Based on Requirements)
            builder.HasIndex(t => new { t.CustomerId, t.CreatedAt });
            builder.HasIndex(t => new { t.Status, t.Priority, t.CreatedAt });
            builder.HasIndex(t => t.AssignedAgentId);

            // Category relationship
            builder.HasOne(t => t.Category)
                   .WithMany(c => c.Tickets)
                   .HasForeignKey(t => t.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Agent relationship
            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(t => t.AssignedAgentId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Cascade deletes for Ticket children
            builder.HasMany(t => t.Comments)
                   .WithOne(c => c.Ticket)
                   .HasForeignKey(c => c.TicketId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.StatusHistories)
                   .WithOne(sh => sh.Ticket)
                   .HasForeignKey(sh => sh.TicketId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Assignments)
                   .WithOne(a => a.Ticket)
                   .HasForeignKey(a => a.TicketId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
