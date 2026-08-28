using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.DTOs;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.GetSlaRiskReport;

public record GetSlaRiskReportQuery : IRequest<Result<SlaRiskReportDto>>;

public class GetSlaRiskReportQueryHandler : IRequestHandler<GetSlaRiskReportQuery, Result<SlaRiskReportDto>>
{
    private readonly IAppDbContext _context;

    public GetSlaRiskReportQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SlaRiskReportDto>> Handle(
        GetSlaRiskReportQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var tickets = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.SlaResolutionDueAt != null)
            .Where(t => t.Status != TicketStatus.Closed
                     && t.Status != TicketStatus.Cancelled
                     && t.Status != TicketStatus.Resolved)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Priority,
                t.CreatedAt,
                t.SlaResolutionDueAt,
                t.AssignedAgentId
            })
            .ToListAsync(cancellationToken);

        var agentNames = await _context.AgentProfiles
            .AsNoTracking()
            .ToDictionaryAsync(a => a.UserId, a => a.DisplayName, cancellationToken);

        var atRisk = new List<SlaRiskTicketDto>();
        var breached = new List<SlaRiskTicketDto>();

        foreach (var ticket in tickets)
        {
            var tempTicket = new Domain.Entities.Ticket
            {
                CreatedAt = ticket.CreatedAt,
                SlaResolutionDueAt = ticket.SlaResolutionDueAt
            };

            var slaStatus = SlaCalculationHelper.DeriveStatus(tempTicket, now);
            if (slaStatus is not (SlaStatus.AtRisk or SlaStatus.Breached))
            {
                continue;
            }

            var dto = new SlaRiskTicketDto
            {
                TicketId = ticket.Id,
                Title = ticket.Title,
                Priority = ticket.Priority,
                SlaStatus = slaStatus,
                SlaResolutionDueAt = ticket.SlaResolutionDueAt!.Value,
                MinutesRemaining = (int)(ticket.SlaResolutionDueAt!.Value - now).TotalMinutes,
                AssignedAgent = ticket.AssignedAgentId != null && agentNames.TryGetValue(ticket.AssignedAgentId, out var name)
                    ? name
                    : "Unassigned"
            };

            if (slaStatus == SlaStatus.Breached)
            {
                breached.Add(dto);
            }
            else
            {
                atRisk.Add(dto);
            }
        }

        atRisk = atRisk.OrderBy(t => t.SlaResolutionDueAt).ToList();
        breached = breached.OrderBy(t => t.SlaResolutionDueAt).ToList();

        return Result<SlaRiskReportDto>.Success(new SlaRiskReportDto
        {
            GeneratedAt = now,
            AtRisk = atRisk,
            Breached = breached
        });
    }
}
