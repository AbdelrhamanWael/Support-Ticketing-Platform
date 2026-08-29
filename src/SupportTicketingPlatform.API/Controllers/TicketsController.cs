using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketingPlatform.Application.Commands.AssignTicket;
using SupportTicketingPlatform.Application.Commands.ReassignTicket;
using SupportTicketingPlatform.Application.Commands.Tickets.CancelTicket;
using SupportTicketingPlatform.Application.Commands.Tickets.CreateTicket;
using SupportTicketingPlatform.Application.Commands.Tickets.ReopenTicket;
using SupportTicketingPlatform.Application.Commands.Tickets.SetTicketPriority;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Queries.GetMyCustomerTickets;
using SupportTicketingPlatform.Application.Queries.GetTicketDetails;
using SupportTicketingPlatform.Application.Queries.GetTicketHistory;
using SupportTicketingPlatform.Application.Queries.SearchTickets;
using SupportTicketingPlatform.Domain.Enums;
using System.Security.Claims;

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

    // POST /api/tickets — Customer creates a new ticket
    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result, value => Created($"/api/tickets/{value}", new { TicketId = value }));
    }

    // GET /api/tickets/mine — Customer views their own tickets (TICKET-S02)
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] TicketStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetMyCustomerTicketsQuery(page, pageSize, status));
        return ToActionResult(result, Ok);
    }

    // GET /api/tickets/search — Lead/Admin search tickets (TICKET-R18: cancelled excluded)
    [HttpGet("search")]
    [Authorize(Roles = "SupportLead,Admin")]
    public async Task<IActionResult> SearchTickets(
        [FromQuery] string? keyword,
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new SearchTicketsQuery(keyword, status, priority, categoryId, page, pageSize));
        return ToActionResult(result, Ok);
    }

    // GET /api/tickets/{id} — View details of a single ticket (TICKET-R01: own ticket only for customers)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTicketDetails(int id)
    {
        var result = await _mediator.Send(new GetTicketDetailsQuery(id));
        return ToActionResult(result, Ok);
    }

    // PUT /api/tickets/{id}/cancel — Customer cancels their own ticket (TICKET-S18)
    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> CancelTicket(int id, [FromBody] CancelTicketRequest request)
    {
        var result = await _mediator.Send(new CancelTicketCommand(id, request.Reason));
        return ToActionResult(result, _ => NoContent());
    }

    // PUT /api/tickets/{id}/reopen — Lead/Admin reopens a closed ticket (TICKET-S12)
    [HttpPut("{id:int}/reopen")]
    [Authorize(Roles = "SupportLead,Admin")]
    public async Task<IActionResult> ReopenTicket(int id, [FromBody] ReopenTicketRequest request)
    {
        var result = await _mediator.Send(new ReopenTicketCommand(id, request.Reason));
        return ToActionResult(result, _ => NoContent());
    }

    // PUT /api/tickets/{id}/priority — Lead/Admin sets ticket priority (TICKET-S03)
    [HttpPut("{id:int}/priority")]
    [Authorize(Roles = "SupportLead,Admin")]
    public async Task<IActionResult> SetPriority(int id, [FromBody] SetPriorityRequest request)
    {
        var result = await _mediator.Send(new SetTicketPriorityCommand(id, request.NewPriority));
        return ToActionResult(result, _ => NoContent());
    }

    // PUT /api/tickets/{id}/assign — Lead/Admin assigns ticket to agent (TICKET-S04)
    [HttpPut("{id:int}/assign")]
    [Authorize(Roles = "SupportLead,Admin")]
    public async Task<IActionResult> AssignTicket(int id, [FromBody] AssignTicketRequest request)
    {
        var result = await _mediator.Send(new AssignTicketCommand(id, request.AgentId, request.Note));
        return ToActionResult(result, Ok);
    }

    // PUT /api/tickets/{id}/reassign — Lead/Admin reassigns ticket (TICKET-S05)
    [HttpPut("{id:int}/reassign")]
    [Authorize(Roles = "SupportLead,Admin")]
    public async Task<IActionResult> ReassignTicket(int id, [FromBody] ReassignTicketRequest request)
    {
        var result = await _mediator.Send(new ReassignTicketCommand(id, request.NewAgentId, request.ReassignmentReason));
        return ToActionResult(result, Ok);
    }

    // GET /api/tickets/{id}/history — Ticket status and assignment history
    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        var result = await _mediator.Send(new GetTicketHistoryQuery(id));
        return ToActionResult(result, Ok);
    }

    // PUT /api/tickets/{id}/status — Agent/Admin changes ticket status (TICKET-S10, S11)
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var command = new ChangeTicketStatusCommand
        {
            TicketId = id,
            NewStatus = request.NewStatus,
            UserId = userId,
            Reason = request.Reason
        };

        await _mediator.Send(command);
        return NoContent();
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
public record CancelTicketRequest(string Reason);
public record ReopenTicketRequest(string Reason);
public record SetPriorityRequest(TicketPriority NewPriority);

public class ChangeStatusRequest
{
    public TicketStatus NewStatus { get; set; }
    public string? Reason { get; set; }
}
