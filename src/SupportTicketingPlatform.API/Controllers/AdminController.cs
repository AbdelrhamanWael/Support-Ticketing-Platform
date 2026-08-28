using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
