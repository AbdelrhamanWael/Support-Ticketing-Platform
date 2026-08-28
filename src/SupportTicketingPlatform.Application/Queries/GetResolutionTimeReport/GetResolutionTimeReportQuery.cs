using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.DTOs;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.GetResolutionTimeReport;

public record GetResolutionTimeReportQuery : IRequest<Result<ResolutionTimeReportDto>>;

public class GetResolutionTimeReportQueryHandler
    : IRequestHandler<GetResolutionTimeReportQuery, Result<ResolutionTimeReportDto>>
{
    private readonly IAppDbContext _context;

    public GetResolutionTimeReportQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ResolutionTimeReportDto>> Handle(
        GetResolutionTimeReportQuery request,
        CancellationToken cancellationToken)
    {
        var resolvedTickets = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Status != TicketStatus.Cancelled)
            .Where(t => t.ResolvedAt != null)
            .Where(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed)
            .Select(t => new
            {
                t.AssignedAgentId,
                t.CategoryId,
                t.CreatedAt,
                ResolvedAt = t.ResolvedAt!.Value
            })
            .ToListAsync(cancellationToken);

        var resolvedWithMinutes = resolvedTickets
            .Select(t => new
            {
                t.AssignedAgentId,
                t.CategoryId,
                ResolutionMinutes = (t.ResolvedAt - t.CreatedAt).TotalMinutes
            })
            .ToList();

        var agents = await _context.AgentProfiles
            .AsNoTracking()
            .ToDictionaryAsync(a => a.UserId, a => new { a.Id, a.DisplayName }, cancellationToken);

        var categories = await _context.TicketCategories
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var byAgent = resolvedWithMinutes
            .Where(t => t.AssignedAgentId != null && agents.ContainsKey(t.AssignedAgentId))
            .GroupBy(t => t.AssignedAgentId!)
            .Select(g =>
            {
                var minutes = g.Select(x => (double)x.ResolutionMinutes).ToList();
                var agent = agents[g.Key];

                return new ResolutionTimeByAgentDto
                {
                    AgentId = agent.Id,
                    AgentName = agent.DisplayName,
                    AverageResolutionMinutes = Math.Round(minutes.Average(), 1),
                    MedianResolutionMinutes = Math.Round(SlaCalculationHelper.Median(minutes), 1),
                    TicketCount = minutes.Count
                };
            })
            .OrderBy(a => a.AgentName)
            .ToList();

        var byCategory = resolvedWithMinutes
            .GroupBy(t => t.CategoryId)
            .Select(g =>
            {
                var minutes = g.Select(x => (double)x.ResolutionMinutes).ToList();

                return new ResolutionTimeByCategoryDto
                {
                    CategoryId = g.Key,
                    CategoryName = categories.GetValueOrDefault(g.Key, "Unknown"),
                    AverageResolutionMinutes = Math.Round(minutes.Average(), 1),
                    MedianResolutionMinutes = Math.Round(SlaCalculationHelper.Median(minutes), 1),
                    TicketCount = minutes.Count
                };
            })
            .OrderBy(c => c.CategoryName)
            .ToList();

        return Result<ResolutionTimeReportDto>.Success(new ResolutionTimeReportDto
        {
            ByAgent = byAgent,
            ByCategory = byCategory
        });
    }
}
