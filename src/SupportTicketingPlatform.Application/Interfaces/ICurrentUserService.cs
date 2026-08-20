namespace SupportTicketingPlatform.Application.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? Role { get; }
        bool IsAuthenticated { get; }
    }
}
