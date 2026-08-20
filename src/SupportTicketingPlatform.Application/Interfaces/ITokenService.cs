using SupportTicketingPlatform.Domain.Entities;

namespace SupportTicketingPlatform.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(ApplicationUser user, string role);
    }
}
