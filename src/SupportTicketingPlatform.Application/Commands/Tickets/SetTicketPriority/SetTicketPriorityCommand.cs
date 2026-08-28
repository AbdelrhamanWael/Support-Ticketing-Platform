using MediatR;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Commands.Tickets.SetTicketPriority
{
    // TICKET-S03: SupportLead or Admin changes the priority of a ticket
    public record SetTicketPriorityCommand(int TicketId, TicketPriority NewPriority) : IRequest<Result<bool>>;

    public class SetTicketPriorityCommandHandler : IRequestHandler<SetTicketPriorityCommand, Result<bool>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SetTicketPriorityCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(SetTicketPriorityCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var ticket = await _context.Tickets.FindAsync(new object[] { request.TicketId }, cancellationToken);

            if (ticket == null)
                return Result<bool>.Failure("Ticket not found.", ErrorType.NotFound);

            // Cannot change priority of cancelled or closed tickets
            if (ticket.Status == TicketStatus.Cancelled || ticket.Status == TicketStatus.Closed)
                return Result<bool>.Failure("Cannot change priority of a cancelled or closed ticket.", ErrorType.Conflict);

            // Record priority change in status history for audit (TICKET-R21)
            var history = new TicketStatusHistory
            {
                TicketId = ticket.Id,
                FromStatus = ticket.Status,
                ToStatus = ticket.Status, // Status doesn't change, only priority
                ChangedByUserId = userId!,
                Reason = $"Priority changed from {ticket.Priority} to {request.NewPriority}",
                ChangedAt = DateTime.UtcNow
            };

            ticket.Priority = request.NewPriority;
            ticket.UpdatedAt = DateTime.UtcNow;

            _context.TicketStatusHistories.Add(history);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}
