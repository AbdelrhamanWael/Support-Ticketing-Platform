using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.DTOs;

public class ConversationDto
{
    public int TicketId { get; set; }
    public IReadOnlyList<ConversationCommentDto> Comments { get; set; } = [];
    public int TotalCount { get; set; }
}

public class ConversationCommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public CommentVisibility? Visibility { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TicketHistoryDto
{
    public int TicketId { get; set; }
    public IReadOnlyList<TicketHistoryEntryDto> History { get; set; } = [];
}

public class TicketHistoryEntryDto
{
    public DateTime ChangedAt { get; set; }
    public string Event { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public class UnassignedTicketDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public double AgeHours { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UnassignedTicketsResult
{
    public IReadOnlyList<UnassignedTicketDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
}
