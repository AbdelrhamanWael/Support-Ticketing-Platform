namespace SupportTicketingPlatform.Domain.Entities;

public class TicketTag
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public string TagName { get; set; } = string.Empty;
}
