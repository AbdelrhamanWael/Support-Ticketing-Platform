using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Domain.Entities;

public class TicketComment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public string AuthorId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public CommentVisibility Visibility { get; set; }

    public DateTime CreatedAt { get; set; }
}
