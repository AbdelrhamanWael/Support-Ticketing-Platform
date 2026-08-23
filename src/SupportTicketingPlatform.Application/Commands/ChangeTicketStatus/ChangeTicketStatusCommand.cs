using MediatR;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;
using SupportTicketingPlatform.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

public class ChangeTicketStatusCommand : IRequest<bool>
{
    public int TicketId { get; set; }
    public TicketStatus NewStatus { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class ChangeTicketStatusCommandHandler : IRequestHandler<ChangeTicketStatusCommand, bool>
{
    private readonly IAppDbContext _context;

    public ChangeTicketStatusCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ChangeTicketStatusCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets.FindAsync(new object[] { request.TicketId }, cancellationToken);
        
        if (ticket == null)
            throw new Exception("Ticket not found");

        // 💡 الشرح للمقابلة: تطبيق قاعدة (TICKET-R09)
        // التحقق من أن الانتقال من الحالة الحالية إلى الحالة الجديدة هو انتقال منطقي ومسموح به.
        if (ticket.Status == TicketStatus.New && request.NewStatus == TicketStatus.Closed)
            throw new InvalidOperationException("TICKET-R09: Cannot change status directly from New to Closed.");

        // تسجيل العملية في جدول الـ History (Audit Trail)
        var statusHistory = new TicketStatusHistory
        {
            TicketId = ticket.Id,
            FromStatus = ticket.Status,
            ToStatus = request.NewStatus,
            ChangedByUserId = request.UserId,
            Reason = request.Reason,
            ChangedAt = DateTime.UtcNow
        };

        // تحديث حالة التذكرة
        ticket.Status = request.NewStatus;
        ticket.UpdatedAt = DateTime.UtcNow;

        _context.TicketStatusHistories.Add(statusHistory);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
