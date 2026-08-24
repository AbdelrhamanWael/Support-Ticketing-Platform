using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SupportTicketingPlatform.API.Controllers
{
    [Route("api/tickets/{ticketId}/comments")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CommentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("public")]
        [Authorize] // مسموح لأي شخص مسجل الدخول (عميل أو موظف)
        public async Task<IActionResult> AddPublicComment(int ticketId, [FromBody] string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var command = new AddPublicCommentCommand { TicketId = ticketId, CommentText = content, UserId = userId };
            
            var commentId = await _mediator.Send(command);
            return Ok(new { Id = commentId, Message = "Public comment added successfully." });
        }

        [HttpPost("internal")]
        [Authorize(Roles = "Agent,Admin")] // تطبيق TICKET-R11: حماية الـ Endpoint للموظفين فقط
        public async Task<IActionResult> AddInternalNote(int ticketId, [FromBody] string content)
        {
            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var command = new AddInternalNoteCommand { TicketId = ticketId, Content = content, StaffId = staffId };
            
            var noteId = await _mediator.Send(command);
            return Ok(new { Id = noteId, Message = "Internal note added successfully." });
        }
    }
}
