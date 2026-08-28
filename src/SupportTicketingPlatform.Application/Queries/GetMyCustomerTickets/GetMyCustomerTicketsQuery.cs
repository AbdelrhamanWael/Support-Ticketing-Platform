using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.GetMyCustomerTickets
{
    // TICKET-S02: Customer views only their own tickets with pagination
    public record GetMyCustomerTicketsQuery(
        int Page = 1,
        int PageSize = 10,
        TicketStatus? StatusFilter = null
    ) : IRequest<Result<PagedResult<CustomerTicketDto>>>;

    // DTO - only shows customer-safe fields (no internal notes, no agent-only data)
    public record CustomerTicketDto(
        int Id,
        string Title,
        string Description,
        string Status,
        string Priority,
        string Category,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    // Wrapper for paginated results
    public record PagedResult<T>(
        List<T> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages
    );

    public class GetMyCustomerTicketsQueryHandler
        : IRequestHandler<GetMyCustomerTicketsQuery, Result<PagedResult<CustomerTicketDto>>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetMyCustomerTicketsQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PagedResult<CustomerTicketDto>>> Handle(
            GetMyCustomerTicketsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrEmpty(userId))
                return Result<PagedResult<CustomerTicketDto>>.Failure("User not authenticated.", ErrorType.Forbidden);

            // TICKET-R01: Filter by current customer's ID only - never returns other customers' tickets
            var query = _context.Tickets
                .Include(t => t.Category)
                .Where(t => t.CustomerId == userId);

            // Optional status filter
            if (request.StatusFilter.HasValue)
                query = query.Where(t => t.Status == request.StatusFilter.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            // Server-side pagination
            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new CustomerTicketDto(
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Status.ToString(),
                    t.Priority.ToString(),
                    t.Category.Name,
                    t.CreatedAt,
                    t.UpdatedAt
                ))
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<CustomerTicketDto>(
                tickets, totalCount, request.Page, request.PageSize, totalPages);

            return Result<PagedResult<CustomerTicketDto>>.Success(pagedResult);
        }
    }
}
