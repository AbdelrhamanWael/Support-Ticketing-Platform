using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.DTOs;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Commands.ReassignTicket;

public record ReassignTicketCommand(int TicketId, int NewAgentId, string ReassignmentReason)
    : IRequest<Result<ReassignmentDto>>;

public class ReassignTicketCommandHandler : IRequestHandler<ReassignTicketCommand, Result<ReassignmentDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ReassignTicketCommandHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ReassignmentDto>> Handle(ReassignTicketCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUser.UserId;
        if (string.IsNullOrEmpty(actorUserId))
        {
            return Result<ReassignmentDto>.Failure("User is not authenticated.", ErrorType.Forbidden);
        }

        if (string.IsNullOrWhiteSpace(request.ReassignmentReason))
        {
            return Result<ReassignmentDto>.Failure(
                "Reassignment reason is required.",
                ErrorType.Validation);
        }

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result<ReassignmentDto>.Failure(
                $"Ticket with ID {request.TicketId} was not found.",
                ErrorType.NotFound);
        }

        if (ticket.Status is TicketStatus.Closed or TicketStatus.Cancelled)
        {
            return Result<ReassignmentDto>.Failure(
                "Cannot reassign a ticket in a terminal status.",
                ErrorType.Validation);
        }

        var activeAssignment = await _context.TicketAssignments
            .FirstOrDefaultAsync(a => a.TicketId == ticket.Id && a.IsActive, cancellationToken);

        if (activeAssignment is null)
        {
            return Result<ReassignmentDto>.Failure(
                "Ticket has no active assignment to reassign.",
                ErrorType.Validation);
        }

        var newAgent = await _context.AgentProfiles
            .FirstOrDefaultAsync(a => a.Id == request.NewAgentId, cancellationToken);

        if (newAgent is null)
        {
            return Result<ReassignmentDto>.Failure(
                $"Agent with ID {request.NewAgentId} was not found.",
                ErrorType.NotFound);
        }

        if (!newAgent.IsActive)
        {
            return Result<ReassignmentDto>.Failure(
                $"Agent with ID {request.NewAgentId} is not active and cannot receive assignments.",
                ErrorType.Validation);
        }

        if (activeAssignment.AgentId == newAgent.UserId)
        {
            return Result<ReassignmentDto>.Failure(
                "Ticket is already assigned to this agent.",
                ErrorType.Validation);
        }

        var previousAgent = await _context.AgentProfiles
            .FirstOrDefaultAsync(a => a.UserId == activeAssignment.AgentId, cancellationToken);

        var previousAgentName = previousAgent?.DisplayName ?? "Unknown";
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            activeAssignment.IsActive = false;
            activeAssignment.EndedAt = now;

            var newAssignment = new TicketAssignment
            {
                TicketId = ticket.Id,
                AgentId = newAgent.UserId,
                AssignedByUserId = actorUserId,
                AssignedAt = now,
                IsActive = true,
                AssignmentNote = request.ReassignmentReason,
                CreatedAt = now
            };

            _context.TicketAssignments.Add(newAssignment);

            ticket.AssignedAgentId = newAgent.UserId;
            ticket.UpdatedAt = now;

            if (ticket.Status == TicketStatus.Reopened)
            {
                ticket.Status = TicketStatus.Assigned;
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                ActorUserId = actorUserId,
                EntityName = nameof(Ticket),
                EntityId = ticket.Id.ToString(),
                ActionType = "TicketReassigned",
                OldValues = $"{{\"agentId\":\"{activeAssignment.AgentId}\",\"agentName\":\"{previousAgentName}\"}}",
                NewValues = $"{{\"agentId\":\"{newAgent.UserId}\",\"agentName\":\"{newAgent.DisplayName}\"}}",
                OccurredAt = now
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<ReassignmentDto>.Success(new ReassignmentDto
            {
                TicketId = ticket.Id,
                PreviousAgent = previousAgentName,
                NewAgent = newAgent.DisplayName,
                ReassignedAt = now
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
