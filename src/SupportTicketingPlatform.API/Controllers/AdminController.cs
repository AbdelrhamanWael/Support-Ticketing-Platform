using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketingPlatform.Application.Commands.ConfigureSlaPolicy;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Queries.GetUnassignedTickets;

namespace SupportTicketingPlatform.API.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(Roles = "SupportLead,Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("tickets/unassigned")]
    public async Task<IActionResult> GetUnassignedTickets()
    {
        var result = await _mediator.Send(new GetUnassignedTicketsQuery());

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new { Error = result.Error });
    }

    [HttpPost("sla-policies")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ConfigureSlaPolicy([FromBody] ConfigureSlaPolicyCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            return Created($"/api/admin/sla-policies/{result.Value!.Id}", result.Value);
        }

        return result.Type switch
        {
            ErrorType.NotFound => NotFound(new { Error = result.Error }),
            ErrorType.Forbidden => new ForbidResult(),
            _ => BadRequest(new { Error = result.Error })
        };
    }
}
