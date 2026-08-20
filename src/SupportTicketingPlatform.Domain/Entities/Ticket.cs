using SupportTicketingPlatform.Domain.Common;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Domain.Entities
{
    public class Ticket : BaseEntity
    {
        public string CustomerId { get; set; } = null!; // FK mapped to ApplicationUser

        public int CategoryId { get; set; }
        public TicketCategory Category { get; set; } = null!;

        public string? AssignedAgentId { get; set; } // Nullable FK mapped to ApplicationUser
        
        public int? SupportTeamId { get; set; }
        public SupportTeam? SupportTeam { get; set; }

        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public TicketStatus Status { get; set; } = TicketStatus.New;
        public SlaStatus SlaStatus { get; set; } = SlaStatus.OnTrack;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? SlaResponseDueAt { get; set; }
        public DateTime? SlaResolutionDueAt { get; set; }

        public string? CancellationReason { get; set; }
        public string? ResolutionNote { get; set; }

        public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
        public ICollection<TicketAttachmentMetadata> Attachments { get; set; } = new List<TicketAttachmentMetadata>();
        public ICollection<TicketAssignment> Assignments { get; set; } = new List<TicketAssignment>();
        public ICollection<TicketStatusHistory> StatusHistories { get; set; } = new List<TicketStatusHistory>();
        public ICollection<TicketTag> Tags { get; set; } = new List<TicketTag>();
    }
}
