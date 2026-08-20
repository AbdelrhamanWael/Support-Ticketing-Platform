namespace SupportTicketingPlatform.Application.Common
{
    public enum ErrorType
    {
        None = 0,
        Validation = 1,  // 400 Bad Request
        NotFound = 2,    // 404 Not Found
        Conflict = 3,    // 409 Conflict (e.g. email already exists)
        Forbidden = 4    // 403 Forbidden (no permission)
    }
}