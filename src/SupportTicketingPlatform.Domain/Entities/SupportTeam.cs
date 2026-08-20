using SupportTicketingPlatform.Domain.Common;
namespace SupportTicketingPlatform.Domain.Entities
{
    public class SupportTeam : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public ICollection<AgentProfile> AgentProfiles { get; set; } = new List<AgentProfile>();
    }
}
