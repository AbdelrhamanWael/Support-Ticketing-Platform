using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketingPlatform.Application.Commands.AssignTicket;
using SupportTicketingPlatform.Application.Commands.ReassignTicket;
using SupportTicketingPlatform.Application.Commands.Tickets.CreateTicket;
using SupportTicketingPlatform.Application.Common;

namespace SupportTicketingPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result, value => Created($"/api/tickets/{value}", new { TicketId = value }));
    }

    [HttpPut("{id:int}/assign")]
    [Authorize(Roles = "SupportLead,Admin")]
    public async Task<IActionResult> AssignTicket(int id, [FromBody] AssignTicketRequest request)
    {
        var result = await _mediator.Send(new AssignTicketCommand(id, request.AgentId, request.Note));
        return ToActionResult(result, Ok);
    }

    [HttpPut("{id:int}/reassign")]
    [Authorize(Roles = "SupportLead,Admin")]
    public async Task<IActionResult> ReassignTicket(int id, [FromBody] ReassignTicketRequest request)
    {
        var result = await _mediator.Send(new ReassignTicketCommand(id, request.NewAgentId, request.ReassignmentReason));
        return ToActionResult(result, Ok);
    }

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        return result.Type switch
        {
            ErrorType.NotFound => NotFound(new { Error = result.Error }),
            ErrorType.Forbidden => new ForbidResult(),
            ErrorType.Conflict => Conflict(new { Error = result.Error }),
            _ => BadRequest(new { Error = result.Error })
        };
    }
}

public record AssignTicketRequest(int AgentId, string? Note);

public record ReassignTicketRequest(int NewAgentId, string ReassignmentReason);
