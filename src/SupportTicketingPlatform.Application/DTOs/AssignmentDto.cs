using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.DTOs;

public class AssignmentDto
{
    public int TicketId { get; set; }
    public string AssignedAgent { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public TicketStatus TicketStatus { get; set; }
}

public class ReassignmentDto
{
    public int TicketId { get; set; }
    public string PreviousAgent { get; set; } = string.Empty;
    public string NewAgent { get; set; } = string.Empty;
    public DateTime ReassignedAt { get; set; }
}

public class AgentQueueTicketDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public SlaStatus SlaStatus { get; set; }
    public DateTime? SlaResolutionDueAt { get; set; }
    public string CustomerDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AgentQueueResult
{
    public string AgentName { get; set; } = string.Empty;
    public IReadOnlyList<AgentQueueTicketDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
