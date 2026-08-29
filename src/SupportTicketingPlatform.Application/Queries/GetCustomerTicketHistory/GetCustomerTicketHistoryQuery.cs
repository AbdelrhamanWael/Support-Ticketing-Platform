using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.GetCustomerTicketHistory;

public record GetCustomerTicketHistoryQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<CustomerTicketHistoryResult>>;

public record CustomerTicketHistoryItemDto(
    int TicketId,
    string TicketTitle,
    string TicketStatus,
    DateTime ChangedAt,
    string Event,
    string Details);

public record CustomerTicketHistoryResult(
    IReadOnlyList<CustomerTicketHistoryItemDto> History,
    int TotalCount,
    int Page,
    int PageSize);

public class GetCustomerTicketHistoryQueryHandler
    : IRequestHandler<GetCustomerTicketHistoryQuery, Result<CustomerTicketHistoryResult>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCustomerTicketHistoryQueryHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CustomerTicketHistoryResult>> Handle(
        GetCustomerTicketHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result<CustomerTicketHistoryResult>.Failure("User is not authenticated.", ErrorType.Forbidden);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 50);

        var tickets = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.CustomerId == userId)
            .Select(t => new { t.Id, t.Title, t.Status, t.CreatedAt })
            .ToListAsync(cancellationToken);

        var ticketIds = tickets.Select(t => t.Id).ToList();

        var statusHistory = await _context.TicketStatusHistories
            .AsNoTracking()
            .Where(h => ticketIds.Contains(h.TicketId))
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);

        var history = new List<CustomerTicketHistoryItemDto>();

        foreach (var ticket in tickets)
        {
            history.Add(new CustomerTicketHistoryItemDto(
                ticket.Id,
                ticket.Title,
                ticket.Status.ToString(),
                ticket.CreatedAt,
                "Created",
                "Ticket submitted"));
        }

        foreach (var entry in statusHistory)
        {
            var ticket = tickets.First(t => t.Id == entry.TicketId);
            history.Add(new CustomerTicketHistoryItemDto(
                entry.TicketId,
                ticket.Title,
                entry.ToStatus.ToString(),
                entry.ChangedAt,
                entry.ToStatus.ToString(),
                $"Status changed to {entry.ToStatus}"));
        }

        history = history.OrderByDescending(h => h.ChangedAt).ToList();
        var totalCount = history.Count;

        var pageItems = history
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Result<CustomerTicketHistoryResult>.Success(
            new CustomerTicketHistoryResult(pageItems, totalCount, page, pageSize));
    }
}
