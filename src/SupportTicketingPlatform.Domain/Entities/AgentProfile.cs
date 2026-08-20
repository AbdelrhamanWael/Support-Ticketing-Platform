using SupportTicketingPlatform.Domain.Common;

namespace SupportTicketingPlatform.Domain.Entities
{
    public class AgentProfile : BaseEntity
    {
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public int? SupportTeamId { get; set; }
        public SupportTeam? SupportTeam { get; set; }

        public string DisplayName { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}
