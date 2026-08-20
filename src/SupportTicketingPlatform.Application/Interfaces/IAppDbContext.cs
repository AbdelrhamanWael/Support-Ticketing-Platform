using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Domain.Entities;

namespace SupportTicketingPlatform.Application.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<Ticket> Tickets { get; }
        // Add other DbSets here as needed for CQRS
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
