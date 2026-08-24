using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketingPlatform.Application.Commands.Categories.CreateCategory;

namespace SupportTicketingPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (result.IsSuccess)
        {
            return Ok(new { CategoryId = result.Value });
        }
        
        return BadRequest(new { Error = result.Error });
    }
}
