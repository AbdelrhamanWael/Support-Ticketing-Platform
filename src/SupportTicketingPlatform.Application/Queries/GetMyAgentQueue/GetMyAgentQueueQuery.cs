using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.DTOs;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.GetMyAgentQueue;

public record GetMyAgentQueueQuery(
    TicketStatus? Status = null,
    SlaStatus? SlaStatus = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<AgentQueueResult>>;

public class GetMyAgentQueueQueryHandler : IRequestHandler<GetMyAgentQueueQuery, Result<AgentQueueResult>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyAgentQueueQueryHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<AgentQueueResult>> Handle(GetMyAgentQueueQuery request, CancellationToken cancellationToken)
    {
        var agentUserId = _currentUser.UserId;
        if (string.IsNullOrEmpty(agentUserId))
        {
            return Result<AgentQueueResult>.Failure("User is not authenticated.", ErrorType.Forbidden);
        }

        var agentProfile = await _context.AgentProfiles
            .FirstOrDefaultAsync(a => a.UserId == agentUserId, cancellationToken);

        if (agentProfile is null)
        {
            return Result<AgentQueueResult>.Failure("Agent profile not found.", ErrorType.NotFound);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 50);

        var query = _context.Tickets
            .Where(t => t.Assignments.Any(a => a.IsActive && a.AgentId == agentUserId))
            .Where(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Cancelled);

        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        if (request.SlaStatus.HasValue)
        {
            query = query.Where(t => t.SlaStatus == request.SlaStatus.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.SlaResolutionDueAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new AgentQueueTicketDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                Priority = t.Priority,
                SlaStatus = t.SlaStatus,
                SlaResolutionDueAt = t.SlaResolutionDueAt,
                CustomerDisplayName = _context.CustomerProfiles
                    .Where(c => c.UserId == t.CustomerId)
                    .Select(c => c.DisplayName)
                    .FirstOrDefault() ?? "Unknown",
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<AgentQueueResult>.Success(new AgentQueueResult
        {
            AgentName = agentProfile.DisplayName,
            Data = data,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }
}
