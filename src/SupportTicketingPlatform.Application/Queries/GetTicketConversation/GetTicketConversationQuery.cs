using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.DTOs;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.GetTicketConversation;

public record GetTicketConversationQuery(int TicketId) : IRequest<Result<ConversationDto>>;

public class GetTicketConversationQueryHandler : IRequestHandler<GetTicketConversationQuery, Result<ConversationDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTicketConversationQueryHandler(IAppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ConversationDto>> Handle(GetTicketConversationQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var role = _currentUser.Role;

        if (string.IsNullOrEmpty(userId))
        {
            return Result<ConversationDto>.Failure("User is not authenticated.", ErrorType.Forbidden);
        }

        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result<ConversationDto>.Failure($"Ticket with ID {request.TicketId} was not found.", ErrorType.NotFound);
        }

        if (!await TicketAccessHelper.CanAccessTicketAsync(_context, ticket, userId, role, cancellationToken: cancellationToken))
        {
            return Result<ConversationDto>.Failure("You do not have access to this ticket.", ErrorType.Forbidden);
        }

        var includeInternal = role != "Customer" && TicketAccessHelper.IsStaff(role);

        var commentsQuery = _context.TicketComments
            .AsNoTracking()
            .Where(c => c.TicketId == request.TicketId);

        if (!includeInternal)
        {
            commentsQuery = commentsQuery.Where(c => c.Visibility == CommentVisibility.Public);
        }

        var comments = await commentsQuery
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Content,
                c.Visibility,
                c.AuthorId,
                c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var authorIds = comments.Select(c => c.AuthorId).Distinct().ToList();
        var customerNames = await _context.CustomerProfiles
            .AsNoTracking()
            .Where(c => authorIds.Contains(c.UserId))
            .ToDictionaryAsync(c => c.UserId, c => c.DisplayName, cancellationToken);

        var agentNames = await _context.AgentProfiles
            .AsNoTracking()
            .Where(a => authorIds.Contains(a.UserId))
            .ToDictionaryAsync(a => a.UserId, a => a.DisplayName, cancellationToken);

        var commentDtos = comments.Select(c =>
        {
            var isCustomer = customerNames.ContainsKey(c.AuthorId);
            var authorName = isCustomer
                ? customerNames[c.AuthorId]
                : agentNames.GetValueOrDefault(c.AuthorId, "Staff");

            return new ConversationCommentDto
            {
                Id = c.Id,
                Content = c.Content,
                Visibility = includeInternal ? c.Visibility : null,
                AuthorName = authorName,
                AuthorRole = isCustomer ? "Customer" : "Agent",
                CreatedAt = c.CreatedAt
            };
        }).ToList();

        return Result<ConversationDto>.Success(new ConversationDto
        {
            TicketId = request.TicketId,
            Comments = commentDtos,
            TotalCount = commentDtos.Count
        });
    }
}
