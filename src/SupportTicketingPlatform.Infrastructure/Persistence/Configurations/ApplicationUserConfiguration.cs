using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketingPlatform.Domain.Entities;

namespace SupportTicketingPlatform.Infrastructure.Persistence.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            // One-to-One with CustomerProfile
            builder.HasOne(u => u.CustomerProfile)
                   .WithOne(c => c.User)
                   .HasForeignKey<CustomerProfile>(c => c.UserId)
                   .OnDelete(DeleteBehavior.Restrict); // TICKET-R23

            // One-to-One with AgentProfile
            builder.HasOne(u => u.AgentProfile)
                   .WithOne(a => a.User)
                   .HasForeignKey<AgentProfile>(a => a.UserId)
                   .OnDelete(DeleteBehavior.Restrict); // TICKET-R23
        }
    }
}
