using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.SearchTickets;

public record SearchTicketsQuery(
    string? Keyword = null,
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    int? CategoryId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<SearchTicketsResult>>;

public record SearchTicketItemDto(
    int Id,
    string Title,
    string Status,
    string Priority,
    string Category,
    string CustomerName,
    string? AssignedAgent,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SearchTicketsResult(
    IReadOnlyList<SearchTicketItemDto> Data,
    int TotalCount,
    int Page,
    int PageSize);

public class SearchTicketsQueryHandler : IRequestHandler<SearchTicketsQuery, Result<SearchTicketsResult>>
{
    private readonly IAppDbContext _context;

    public SearchTicketsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SearchTicketsResult>> Handle(SearchTicketsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 50);

        var query = _context.Tickets
            .AsNoTracking()
            .Where(t => t.Status != TicketStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(t => t.Title.Contains(keyword) || t.Description.Contains(keyword));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == request.Priority.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == request.CategoryId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new SearchTicketItemDto(
                t.Id,
                t.Title,
                t.Status.ToString(),
                t.Priority.ToString(),
                _context.TicketCategories.Where(c => c.Id == t.CategoryId).Select(c => c.Name).FirstOrDefault() ?? "Unknown",
                _context.CustomerProfiles.Where(c => c.UserId == t.CustomerId).Select(c => c.DisplayName).FirstOrDefault() ?? "Unknown",
                t.AssignedAgentId != null
                    ? _context.AgentProfiles.Where(a => a.UserId == t.AssignedAgentId).Select(a => a.DisplayName).FirstOrDefault()
                    : null,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<SearchTicketsResult>.Success(new SearchTicketsResult(data, totalCount, page, pageSize));
    }
}
