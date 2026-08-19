namespace SupportTicketingPlatform.Domain.Entities;

public class ActivityLog
{
    public int Id { get; set; }

    public string ActorUserId { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string ActionType { get; set; } = string.Empty;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? IpAddress { get; set; }
}
