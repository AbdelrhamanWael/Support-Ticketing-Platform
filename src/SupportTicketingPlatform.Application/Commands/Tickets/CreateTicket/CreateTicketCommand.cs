using MediatR;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Commands.Tickets.CreateTicket
{
    // The Command Input
    public record CreateTicketCommand(string Title, string Description, TicketPriority Priority, int CategoryId) : IRequest<Result<int>>;

    // The Handler
    public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Result<int>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateTicketCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<int>> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<int>.Failure("User is not authenticated.", ErrorType.Forbidden);
            }

            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Status = TicketStatus.New, // From Domain Enums
                CategoryId = request.CategoryId,
                CustomerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<int>.Success(ticket.Id);
        }
    }
}
