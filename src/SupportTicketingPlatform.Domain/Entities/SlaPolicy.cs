using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Domain.Entities;

public class SlaPolicy
{
    public int Id { get; set; }

    public int? TicketCategoryId { get; set; }

    public TicketPriority Priority { get; set; }

    public int ResponseTargetMinutes { get; set; }

    public int ResolutionTargetMinutes { get; set; }

    public bool IsActive { get; set; }
}
