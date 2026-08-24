using MediatR;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;
using SupportTicketingPlatform.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

public class AddPublicCommentCommand : IRequest<int>
{
    public int TicketId { get; set; }           // Fix: int not string
    public string CommentText { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public class AddPublicCommentCommandHandler : IRequestHandler<AddPublicCommentCommand, int>
{
    private readonly IAppDbContext _context;

    public AddPublicCommentCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(AddPublicCommentCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets.FindAsync(new object[] { request.TicketId }, cancellationToken);

        if (ticket == null)
            throw new Exception("Ticket Not Found");

        // TICKET-R10: لا يمكن إضافة تعليق على تذكرة مغلقة
        if (ticket.Status == TicketStatus.Closed)
            throw new InvalidOperationException("TICKET-R10: Cannot add a comment to a closed ticket.");

        var comment = new TicketComment
        {
            TicketId = request.TicketId,        // Fix: int now
            Content = request.CommentText,      // Fix: CommentText not Comment
            AuthorId = request.UserId,
            Visibility = CommentVisibility.Public,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketComments.Add(comment);   // Fix: TicketComments not Comments
        await _context.SaveChangesAsync(cancellationToken);
        return comment.Id;
    }
}