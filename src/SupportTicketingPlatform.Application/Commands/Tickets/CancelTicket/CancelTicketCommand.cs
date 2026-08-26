using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Commands.Tickets.CancelTicket
{
    // TICKET-S18: Customer cancels their own ticket (only if New or Assigned)
    public record CancelTicketCommand(int TicketId, string Reason) : IRequest<Result<bool>>;

    public class CancelTicketCommandHandler : IRequestHandler<CancelTicketCommand, Result<bool>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CancelTicketCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(CancelTicketCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var ticket = await _context.Tickets.FindAsync(new object[] { request.TicketId }, cancellationToken);

            if (ticket == null)
                return Result<bool>.Failure("Ticket not found.", ErrorType.NotFound);

            // TICKET-R01: Customer can only cancel their own ticket
            if (ticket.CustomerId != userId)
                return Result<bool>.Failure("You can only cancel your own tickets.", ErrorType.Forbidden);

            // TICKET-R16: Cancellation only allowed in early statuses (New or Assigned)
            var allowedStatuses = new[] { TicketStatus.New, TicketStatus.Assigned };
            if (!allowedStatuses.Contains(ticket.Status))
                return Result<bool>.Failure("Ticket can only be cancelled when it is New or Assigned.", ErrorType.Conflict);

            // Record the status change in history for audit trail
            var history = new TicketStatusHistory
            {
                TicketId = ticket.Id,
                FromStatus = ticket.Status,
                ToStatus = TicketStatus.Cancelled,
                ChangedByUserId = userId!,
                Reason = request.Reason,
                ChangedAt = DateTime.UtcNow
            };

            ticket.Status = TicketStatus.Cancelled;
            ticket.CancellationReason = request.Reason;
            ticket.UpdatedAt = DateTime.UtcNow;

            _context.TicketStatusHistories.Add(history);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}
