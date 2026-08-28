using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.DTOs;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.GetUnassignedTickets;

public record GetUnassignedTicketsQuery : IRequest<Result<UnassignedTicketsResult>>;

public class GetUnassignedTicketsQueryHandler : IRequestHandler<GetUnassignedTicketsQuery, Result<UnassignedTicketsResult>>
{
    private readonly IAppDbContext _context;

    public GetUnassignedTicketsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UnassignedTicketsResult>> Handle(GetUnassignedTicketsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var tickets = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Cancelled)
            .Where(t => !t.Assignments.Any(a => a.IsActive))
            .Where(t => t.AssignedAgentId == null)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new UnassignedTicketDto
            {
                Id = t.Id,
                Title = t.Title,
                Priority = t.Priority,
                CreatedAt = t.CreatedAt,
                AgeHours = 0
            })
            .ToListAsync(cancellationToken);

        foreach (var ticket in tickets)
        {
            ticket.AgeHours = Math.Round((now - ticket.CreatedAt).TotalHours, 1);
        }

        return Result<UnassignedTicketsResult>.Success(new UnassignedTicketsResult
        {
            Data = tickets,
            TotalCount = tickets.Count
        });
    }
}
