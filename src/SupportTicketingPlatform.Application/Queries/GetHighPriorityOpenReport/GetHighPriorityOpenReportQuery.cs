using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.DTOs;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.GetHighPriorityOpenReport;

public record GetHighPriorityOpenReportQuery : IRequest<Result<HighPriorityOpenReportDto>>;

public class GetHighPriorityOpenReportQueryHandler
    : IRequestHandler<GetHighPriorityOpenReportQuery, Result<HighPriorityOpenReportDto>>
{
    private readonly IAppDbContext _context;

    public GetHighPriorityOpenReportQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<HighPriorityOpenReportDto>> Handle(
        GetHighPriorityOpenReportQuery request,
        CancellationToken cancellationToken)
    {
        var openStatuses = new[]
        {
            TicketStatus.New,
            TicketStatus.Assigned,
            TicketStatus.InProgress,
            TicketStatus.Reopened,
            TicketStatus.Resolved
        };

        var tickets = await _context.Tickets
            .AsNoTracking()
            .Where(t => openStatuses.Contains(t.Status))
            .Where(t => t.Priority == TicketPriority.High || t.Priority == TicketPriority.Critical)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new HighPriorityTicketDto
            {
                Id = t.Id,
                Title = t.Title,
                Priority = t.Priority,
                Status = t.Status,
                CreatedAt = t.CreatedAt,
                AssignedAgent = t.AssignedAgentId != null
                    ? _context.AgentProfiles
                        .Where(a => a.UserId == t.AssignedAgentId)
                        .Select(a => a.DisplayName)
                        .FirstOrDefault()
                    : null
            })
            .ToListAsync(cancellationToken);

        return Result<HighPriorityOpenReportDto>.Success(new HighPriorityOpenReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            Tickets = tickets
        });
    }
}
