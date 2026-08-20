using Microsoft.AspNetCore.Identity;

namespace SupportTicketingPlatform.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public CustomerProfile? CustomerProfile { get; set; }
        public AgentProfile? AgentProfile { get; set; }
        
        public ICollection<TicketAssignment> AssignedByTickets { get; set; } = new List<TicketAssignment>();
        public ICollection<TicketStatusHistory> ChangedStatusHistories { get; set; } = new List<TicketStatusHistory>();
        public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
        
        public bool IsDeleted { get; set; } = false;
    }
}
