using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Domain.Entities;

public class TicketStatusHistory
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public string ChangedByUserId { get; set; } = string.Empty;

    public TicketStatus FromStatus { get; set; }

    public TicketStatus ToStatus { get; set; }

    public string? Reason { get; set; }

    public DateTime ChangedAt { get; set; }
}
