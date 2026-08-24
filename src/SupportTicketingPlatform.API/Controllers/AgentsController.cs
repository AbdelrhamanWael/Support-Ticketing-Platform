using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Queries.GetMyAgentQueue;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.API.Controllers;

[Route("api/agents")]
[ApiController]
[Authorize(Roles = "SupportAgent")]
public class AgentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me/tickets")]
    public async Task<IActionResult> GetMyQueue(
        [FromQuery] TicketStatus? status,
        [FromQuery] SlaStatus? slaStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetMyAgentQueueQuery(status, slaStatus, page, pageSize));

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
