using SupportTicketingPlatform.Domain.Common;

namespace SupportTicketingPlatform.Domain.Entities
{
    public class TicketAssignment : BaseEntity
    {
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;

        public string AgentId { get; set; } = null!; // FK string mapped to ApplicationUser
        public string? AssignedByUserId { get; set; }
        public ApplicationUser? AssignedByUser { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        public string? AssignmentNote { get; set; }
    }
}
