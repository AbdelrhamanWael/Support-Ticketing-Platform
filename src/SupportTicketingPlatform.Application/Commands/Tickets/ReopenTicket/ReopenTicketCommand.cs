using MediatR;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Commands.Tickets.ReopenTicket
{
    // TICKET-S12: SupportLead or Admin reopens a closed ticket
    public record ReopenTicketCommand(int TicketId, string Reason) : IRequest<Result<bool>>;

    public class ReopenTicketCommandHandler : IRequestHandler<ReopenTicketCommand, Result<bool>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ReopenTicketCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(ReopenTicketCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var ticket = await _context.Tickets.FindAsync(new object[] { request.TicketId }, cancellationToken);

            if (ticket == null)
                return Result<bool>.Failure("Ticket not found.", ErrorType.NotFound);

            // TICKET-R15: Only Closed tickets can be reopened
            if (ticket.Status != TicketStatus.Closed && ticket.Status != TicketStatus.Resolved)
                return Result<bool>.Failure("Only Closed or Resolved tickets can be reopened.", ErrorType.Conflict);

            // Record history for audit trail
            var history = new TicketStatusHistory
            {
                TicketId = ticket.Id,
                FromStatus = ticket.Status,
                ToStatus = TicketStatus.Reopened,
                ChangedByUserId = userId!,
                Reason = request.Reason,
                ChangedAt = DateTime.UtcNow
            };

            ticket.Status = TicketStatus.Reopened;
            ticket.UpdatedAt = DateTime.UtcNow;
            // Clear resolved/closed timestamps since it's reopened
            ticket.ResolvedAt = null;
            ticket.ClosedAt = null;

            _context.TicketStatusHistories.Add(history);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}
