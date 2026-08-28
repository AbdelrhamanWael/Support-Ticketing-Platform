using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.DTOs;

public class TicketsByStatusReportDto
{
    public DateRangeDto DateRange { get; set; } = new();
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> ByPriority { get; set; } = new();
}

public class DateRangeDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class AgentWorkloadReportDto
{
    public DateTime GeneratedAt { get; set; }
    public IReadOnlyList<AgentWorkloadEntryDto> Agents { get; set; } = [];
}

public class AgentWorkloadEntryDto
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string? Team { get; set; }
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int CriticalTickets { get; set; }
}

public class HighPriorityOpenReportDto
{
    public DateTime GeneratedAt { get; set; }
    public IReadOnlyList<HighPriorityTicketDto> Tickets { get; set; } = [];
}

public class HighPriorityTicketDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AssignedAgent { get; set; }
}

public class ResolutionTimeReportDto
{
    public IReadOnlyList<ResolutionTimeByAgentDto> ByAgent { get; set; } = [];
    public IReadOnlyList<ResolutionTimeByCategoryDto> ByCategory { get; set; } = [];
}

public class ResolutionTimeByAgentDto
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public double AverageResolutionMinutes { get; set; }
    public double MedianResolutionMinutes { get; set; }
    public int TicketCount { get; set; }
}

public class ResolutionTimeByCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double AverageResolutionMinutes { get; set; }
    public double MedianResolutionMinutes { get; set; }
    public int TicketCount { get; set; }
}

public class SlaRiskReportDto
{
    public DateTime GeneratedAt { get; set; }
    public IReadOnlyList<SlaRiskTicketDto> AtRisk { get; set; } = [];
    public IReadOnlyList<SlaRiskTicketDto> Breached { get; set; } = [];
}

public class SlaRiskTicketDto
{
    public int TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public SlaStatus SlaStatus { get; set; }
    public DateTime SlaResolutionDueAt { get; set; }
    public int MinutesRemaining { get; set; }
    public string AssignedAgent { get; set; } = string.Empty;
}
