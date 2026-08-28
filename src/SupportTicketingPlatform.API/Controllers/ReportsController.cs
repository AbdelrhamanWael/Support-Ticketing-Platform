using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Queries.GetAgentWorkloadReport;
using SupportTicketingPlatform.Application.Queries.GetHighPriorityOpenReport;
using SupportTicketingPlatform.Application.Queries.GetResolutionTimeReport;
using SupportTicketingPlatform.Application.Queries.GetSlaRiskReport;
using SupportTicketingPlatform.Application.Queries.GetTicketsByStatusReport;

namespace SupportTicketingPlatform.API.Controllers;

[Route("api/reports")]
[ApiController]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("tickets-by-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTicketsByStatus(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        var result = await _mediator.Send(new GetTicketsByStatusReportQuery(dateFrom, dateTo));
        return ToActionResult(result);
    }

    [HttpGet("agent-workload")]
    [Authorize(Roles = "SupportLead,Admin")]
    public async Task<IActionResult> GetAgentWorkload()
    {
        var result = await _mediator.Send(new GetAgentWorkloadReportQuery());
        return ToActionResult(result);
    }

    [HttpGet("high-priority-open")]
    [Authorize(Roles = "SupportLead,Admin")]
    public async Task<IActionResult> GetHighPriorityOpen()
    {
        var result = await _mediator.Send(new GetHighPriorityOpenReportQuery());
        return ToActionResult(result);
    }

    [HttpGet("resolution-time")]
    [Authorize(Roles = "SupportLead,Admin")]
    public async Task<IActionResult> GetResolutionTime()
    {
        var result = await _mediator.Send(new GetResolutionTimeReportQuery());
        return ToActionResult(result);
    }

    [HttpGet("sla-risk")]
    [Authorize(Roles = "SupportLead,Admin")]
    public async Task<IActionResult> GetSlaRisk()
    {
        var result = await _mediator.Send(new GetSlaRiskReportQuery());
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Type switch
        {
            ErrorType.NotFound => NotFound(new { Error = result.Error }),
            ErrorType.Forbidden => new ForbidResult(),
            _ => BadRequest(new { Error = result.Error })
        };
    }
}
