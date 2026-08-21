using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.DTOs;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Commands.AssignTicket;

public record AssignTicketCommand(int TicketId, int AgentId, string? Note) : IRequest<Result<AssignmentDto>>;

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, Result<AssignmentDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AssignTicketCommandHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<AssignmentDto>> Handle(AssignTicketCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUser.UserId;
        if (string.IsNullOrEmpty(actorUserId))
        {
            return Result<AssignmentDto>.Failure("User is not authenticated.", ErrorType.Forbidden);
        }

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result<AssignmentDto>.Failure($"Ticket with ID {request.TicketId} was not found.", ErrorType.NotFound);
        }

        if (ticket.Status is TicketStatus.Closed or TicketStatus.Cancelled)
        {
            return Result<AssignmentDto>.Failure(
                "Cannot assign a ticket in a terminal status.",
                ErrorType.Validation);
        }

        var hasActiveAssignment = await _context.TicketAssignments
            .AnyAsync(a => a.TicketId == ticket.Id && a.IsActive, cancellationToken);

        if (hasActiveAssignment)
        {
            return Result<AssignmentDto>.Failure(
                "Ticket already has an active assignment. Use reassign instead.",
                ErrorType.Conflict);
        }

        var agent = await _context.AgentProfiles
            .FirstOrDefaultAsync(a => a.Id == request.AgentId, cancellationToken);

        if (agent is null)
        {
            return Result<AssignmentDto>.Failure(
                $"Agent with ID {request.AgentId} was not found.",
                ErrorType.NotFound);
        }

        if (!agent.IsActive)
        {
            return Result<AssignmentDto>.Failure(
                $"Agent with ID {request.AgentId} is not active and cannot receive assignments.",
                ErrorType.Validation);
        }

        var now = DateTime.UtcNow;
        var previousStatus = ticket.Status;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var assignment = new TicketAssignment
            {
                TicketId = ticket.Id,
                AgentId = agent.UserId,
                AssignedByUserId = actorUserId,
                AssignedAt = now,
                IsActive = true,
                AssignmentNote = request.Note,
                CreatedAt = now
            };

            _context.TicketAssignments.Add(assignment);

            ticket.AssignedAgentId = agent.UserId;
            ticket.UpdatedAt = now;

            if (ticket.Status == TicketStatus.New)
            {
                ticket.Status = TicketStatus.Assigned;

                _context.TicketStatusHistories.Add(new TicketStatusHistory
                {
                    TicketId = ticket.Id,
                    ChangedByUserId = actorUserId,
                    FromStatus = previousStatus,
                    ToStatus = TicketStatus.Assigned,
                    Reason = request.Note,
                    ChangedAt = now
                });
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                ActorUserId = actorUserId,
                EntityName = nameof(Ticket),
                EntityId = ticket.Id.ToString(),
                ActionType = "TicketAssigned",
                NewValues = $"{{\"agentId\":\"{agent.UserId}\",\"agentName\":\"{agent.DisplayName}\"}}",
                OccurredAt = now
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<AssignmentDto>.Success(new AssignmentDto
            {
                TicketId = ticket.Id,
                AssignedAgent = agent.DisplayName,
                AssignedAt = now,
                TicketStatus = ticket.Status
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
