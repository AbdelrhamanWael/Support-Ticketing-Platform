using SupportTicketingPlatform.Domain.Common;

namespace SupportTicketingPlatform.Domain.Entities
{
    public class CustomerProfile : BaseEntity
    {
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Company { get; set; }
    }
}