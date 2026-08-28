using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SupportTicketingPlatform.Domain.Entities;

namespace SupportTicketingPlatform.Application.Interfaces;

public interface IAppDbContext
{
    DatabaseFacade Database { get; }

    DbSet<Ticket> Tickets { get; }
    DbSet<TicketComment> TicketComments { get; }
    DbSet<TicketAssignment> TicketAssignments { get; }
    DbSet<AgentProfile> AgentProfiles { get; }
    DbSet<CustomerProfile> CustomerProfiles { get; }
    DbSet<TicketStatusHistory> TicketStatusHistories { get; }
    DbSet<ActivityLog> ActivityLogs { get; }
    DbSet<TicketCategory> TicketCategories { get; }
    DbSet<SlaPolicy> SlaPolicies { get; }
    DbSet<SupportTeam> SupportTeams { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
