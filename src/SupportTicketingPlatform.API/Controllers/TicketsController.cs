using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketingPlatform.Application.Commands.Tickets.CreateTicket;

namespace SupportTicketingPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires Token for all endpoints in this controller
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
            
            if (result.IsSuccess)
                return Created($"/api/tickets/{result.Value}", new { TicketId = result.Value });

            if (result.Type == Application.Common.ErrorType.Forbidden)
                return Forbid();

            return BadRequest(new { Error = result.Error });
        }
    }
}
