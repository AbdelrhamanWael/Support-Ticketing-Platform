using MediatR;
using Microsoft.AspNetCore.Mvc;
using SupportTicketingPlatform.Application.Commands.Auth.RegisterCustomer;
using SupportTicketingPlatform.Application.Queries.Auth.Login;

namespace SupportTicketingPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCustomerCommand command)
        {
            var result = await _mediator.Send(command);
            
            if (result.IsSuccess)
                return Ok(new { UserId = result.Value });

            return BadRequest(new { Error = result.Error });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginQuery query)
        {
            var result = await _mediator.Send(query);
            
            if (result.IsSuccess)
                return Ok(new { Token = result.Value });

            return Unauthorized(new { Error = result.Error });
        }
    }
}
