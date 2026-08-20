using SupportTicketingPlatform.Domain.Common;

namespace SupportTicketingPlatform.Domain.Entities
{
    public class TicketCategory : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public ICollection<SlaPolicy> SlaPolicies { get; set; } = new List<SlaPolicy>();
    }
}
