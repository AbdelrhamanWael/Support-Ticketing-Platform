using MediatR;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;
using SupportTicketingPlatform.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;


public class AddPublicCommentCommand : IRequest<int>
{
    public string TicketId { get; set; }
    public string CommentText { get; set; }
    public string UserId { get; set; }
}
public class AddPublicCommentCommandHandler : IRequestHandler<AddPublicCommentCommand, int>
{
    private readonly IAppDbContext _context;
    public AddPublicCommentCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(AddPublicCommentCommand request , CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets.FindAsync(new object[] { request.TicketId }, cancellationToken);
        if(ticket == null) 
            throw new Exception("Ticket Not Found");

        if(ticket.Status == TicketStatus.Closed)
            throw new Exception("Cannot add a comment to a closed ticket.");

        var comment = new TicketComment{
            TicketId = request.TicketId,
            Content = request.Comment,
            AuthorId = request.UserId,
            Visibility = CommentVisibility.Public,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);
        return comment.Id;
    }
}