using MediatR;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;
using SupportTicketingPlatform.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

public class AddInternalNoteCommand : IRequest<int>
{
    public int TicketId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string StaffId { get; set; } = string.Empty; // Only Staff
}

public class AddInternalNoteCommandHandler : IRequestHandler<AddInternalNoteCommand, int>
{
    private readonly IAppDbContext _context;

    public AddInternalNoteCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(AddInternalNoteCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets.FindAsync(new object[] { request.TicketId }, cancellationToken);
        
        if (ticket == null)
            throw new Exception("Ticket not found");

        var note = new TicketComment
        {
            TicketId = request.TicketId,
            Content = request.Content,
            AuthorId = request.StaffId,
            Visibility = CommentVisibility.Internal, // 💡 نوع التعليق: ملاحظة داخلية
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketComments.Add(note);
        await _context.SaveChangesAsync(cancellationToken);

        return note.Id;
    }
}
