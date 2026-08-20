using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Application.Interfaces;

namespace SupportTicketingPlatform.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<CustomerProfile> CustomerProfiles { get; set; }
        public DbSet<AgentProfile> AgentProfiles { get; set; }
        public DbSet<SupportTeam> SupportTeams { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketCategory> TicketCategories { get; set; }
        public DbSet<TicketAssignment> TicketAssignments { get; set; }
        public DbSet<TicketComment> TicketComments { get; set; }
        public DbSet<TicketAttachmentMetadata> TicketAttachments { get; set; }
        public DbSet<TicketStatusHistory> TicketStatusHistories { get; set; }
        public DbSet<SlaPolicy> SlaPolicies { get; set; }
        public DbSet<TicketTag> TicketTags { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Apply all configurations defined in the assembly
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
