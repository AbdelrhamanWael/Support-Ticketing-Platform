using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Queries.GetCustomerTicketHistory;

namespace SupportTicketingPlatform.API.Controllers;

[Route("api/customers")]
[ApiController]
[Authorize(Roles = "Customer")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me/history")]
    public async Task<IActionResult> GetMyHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetCustomerTicketHistoryQuery(page, pageSize));

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Type switch
        {
            ErrorType.Forbidden => new ForbidResult(),
            _ => BadRequest(new { Error = result.Error })
        };
    }
}
