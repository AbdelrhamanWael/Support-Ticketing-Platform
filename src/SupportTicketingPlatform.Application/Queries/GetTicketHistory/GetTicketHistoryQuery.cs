using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.DTOs;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.GetTicketHistory;

public record GetTicketHistoryQuery(int TicketId) : IRequest<Result<TicketHistoryDto>>;

public class GetTicketHistoryQueryHandler : IRequestHandler<GetTicketHistoryQuery, Result<TicketHistoryDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTicketHistoryQueryHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<TicketHistoryDto>> Handle(GetTicketHistoryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var role = _currentUser.Role;

        if (string.IsNullOrEmpty(userId))
        {
            return Result<TicketHistoryDto>.Failure("User is not authenticated.", ErrorType.Forbidden);
        }

        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result<TicketHistoryDto>.Failure($"Ticket with ID {request.TicketId} was not found.", ErrorType.NotFound);
        }

        var isCustomer = role == "Customer";
        if (isCustomer && ticket.CustomerId != userId)
        {
            return Result<TicketHistoryDto>.Failure("You do not have access to this ticket.", ErrorType.Forbidden);
        }

        if (!isCustomer && !TicketAccessHelper.IsLeadOrAdmin(role))
        {
            return Result<TicketHistoryDto>.Failure("You do not have access to this ticket history.", ErrorType.Forbidden);
        }

        var history = new List<TicketHistoryEntryDto>
        {
            new()
            {
                ChangedAt = ticket.CreatedAt,
                Event = "Created",
                ChangedBy = await ResolveDisplayNameAsync(ticket.CustomerId, cancellationToken),
                Details = "Status set to New"
            }
        };

        var statusHistory = await _context.TicketStatusHistories
            .AsNoTracking()
            .Where(h => h.TicketId == request.TicketId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(cancellationToken);

        foreach (var entry in statusHistory)
        {
            history.Add(new TicketHistoryEntryDto
            {
                ChangedAt = entry.ChangedAt,
                Event = entry.ToStatus.ToString(),
                ChangedBy = await ResolveDisplayNameAsync(entry.ChangedByUserId, cancellationToken),
                Details = isCustomer
                    ? $"Status changed to {entry.ToStatus}"
                    : entry.Reason ?? $"Status changed from {entry.FromStatus} to {entry.ToStatus}"
            });
        }

        var assignments = await _context.TicketAssignments
            .AsNoTracking()
            .Where(a => a.TicketId == request.TicketId)
            .OrderBy(a => a.AssignedAt)
            .ToListAsync(cancellationToken);

        foreach (var assignment in assignments)
        {
            var agentName = await ResolveDisplayNameAsync(assignment.AgentId, cancellationToken);
            var assignedBy = assignment.AssignedByUserId is not null
                ? await ResolveDisplayNameAsync(assignment.AssignedByUserId, cancellationToken)
                : "System";

            history.Add(new TicketHistoryEntryDto
            {
                ChangedAt = assignment.AssignedAt,
                Event = assignment.IsActive ? "Assigned" : "Reassigned",
                ChangedBy = assignedBy,
                Details = isCustomer
                    ? $"Assigned to {agentName}"
                    : $"Assigned to {agentName}" + (string.IsNullOrWhiteSpace(assignment.AssignmentNote) ? "" : $" — {assignment.AssignmentNote}")
            });
        }

        history = history.OrderBy(h => h.ChangedAt).ToList();

        return Result<TicketHistoryDto>.Success(new TicketHistoryDto
        {
            TicketId = request.TicketId,
            History = history
        });
    }

    private async Task<string> ResolveDisplayNameAsync(string userId, CancellationToken cancellationToken)
    {
        var customer = await _context.CustomerProfiles
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => c.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrEmpty(customer))
        {
            return customer;
        }

        var agent = await _context.AgentProfiles
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => a.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);

        return agent ?? "System";
    }
}
