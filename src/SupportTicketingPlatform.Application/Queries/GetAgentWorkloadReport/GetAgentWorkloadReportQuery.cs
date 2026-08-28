using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.DTOs;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.GetAgentWorkloadReport;

public record GetAgentWorkloadReportQuery : IRequest<Result<AgentWorkloadReportDto>>;

public class GetAgentWorkloadReportQueryHandler
    : IRequestHandler<GetAgentWorkloadReportQuery, Result<AgentWorkloadReportDto>>
{
    private readonly IAppDbContext _context;

    public GetAgentWorkloadReportQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AgentWorkloadReportDto>> Handle(
        GetAgentWorkloadReportQuery request,
        CancellationToken cancellationToken)
    {
        var agents = await _context.AgentProfiles
            .AsNoTracking()
            .Where(a => a.IsActive)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.DisplayName,
                Team = a.SupportTeamId != null
                    ? _context.SupportTeams.Where(t => t.Id == a.SupportTeamId).Select(t => t.Name).FirstOrDefault()
                    : null
            })
            .ToListAsync(cancellationToken);

        var activeTickets = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Cancelled)
            .Where(t => t.AssignedAgentId != null)
            .Select(t => new { t.AssignedAgentId, t.Status, t.Priority })
            .ToListAsync(cancellationToken);

        var entries = agents.Select(agent =>
        {
            var agentTickets = activeTickets.Where(t => t.AssignedAgentId == agent.UserId).ToList();

            return new AgentWorkloadEntryDto
            {
                AgentId = agent.Id,
                AgentName = agent.DisplayName,
                Team = agent.Team,
                OpenTickets = agentTickets.Count(t => t.Status is TicketStatus.New or TicketStatus.Assigned or TicketStatus.Reopened),
                InProgressTickets = agentTickets.Count(t => t.Status == TicketStatus.InProgress),
                CriticalTickets = agentTickets.Count(t => t.Priority == TicketPriority.Critical)
            };
        }).OrderByDescending(a => a.OpenTickets + a.InProgressTickets).ToList();

        return Result<AgentWorkloadReportDto>.Success(new AgentWorkloadReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            Agents = entries
        });
    }
}
