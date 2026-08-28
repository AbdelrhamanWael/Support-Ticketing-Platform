using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.GetTicketDetails
{
    // Query 04: Get full details of a single ticket
    public record GetTicketDetailsQuery(int TicketId) : IRequest<Result<TicketDetailsDto>>;

    public record TicketDetailsDto(
        int Id,
        string Title,
        string Description,
        string Status,
        string Priority,
        string Category,
        string CustomerName,
        string? AssignedAgentId,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? ResolvedAt,
        DateTime? ClosedAt,
        List<PublicCommentDto> PublicComments  // TICKET-R03: Only public comments returned
    );

    public record PublicCommentDto(
        int Id,
        string Content,
        string AuthorId,
        DateTime CreatedAt
    );

    public class GetTicketDetailsQueryHandler : IRequestHandler<GetTicketDetailsQuery, Result<TicketDetailsDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetTicketDetailsQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<TicketDetailsDto>> Handle(
            GetTicketDetailsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var role = _currentUserService.Role;

            var ticket = await _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Comments)
                .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

            if (ticket == null)
                return Result<TicketDetailsDto>.Failure("Ticket not found.", ErrorType.NotFound);

            // TICKET-R01 & TICKET-R02: Customers see only their own tickets
            bool isCustomer = role == "Customer";
            if (isCustomer && ticket.CustomerId != userId)
                return Result<TicketDetailsDto>.Failure("You are not allowed to view this ticket.", ErrorType.Forbidden);

            // TICKET-R03: Only return Public comments — internal notes are NEVER shown to customers
            var publicComments = ticket.Comments
                .Where(c => c.Visibility == CommentVisibility.Public)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new PublicCommentDto(c.Id, c.Content, c.AuthorId, c.CreatedAt))
                .ToList();

            var dto = new TicketDetailsDto(
                ticket.Id,
                ticket.Title,
                ticket.Description,
                ticket.Status.ToString(),
                ticket.Priority.ToString(),
                ticket.Category.Name,
                ticket.CustomerId,
                ticket.AssignedAgentId,
                ticket.CreatedAt,
                ticket.UpdatedAt,
                ticket.ResolvedAt,
                ticket.ClosedAt,
                publicComments
            );

            return Result<TicketDetailsDto>.Success(dto);
        }
    }
}
