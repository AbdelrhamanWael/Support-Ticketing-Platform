using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;

namespace SupportTicketingPlatform.Application.Common;

public static class TicketAccessHelper
{
    public static bool IsStaff(string? role) =>
        role is "Admin" or "SupportLead" or "Agent";

    public static bool IsLeadOrAdmin(string? role) =>
        role is "Admin" or "SupportLead";

    public static bool IsAgent(string? role) =>
        role is "Agent";

    public static async Task<bool> CanAccessTicketAsync(
        IAppDbContext context,
        Ticket ticket,
        string userId,
        string? role,
        bool requireAssignmentForAgent = true,
        CancellationToken cancellationToken = default)
    {
        if (IsLeadOrAdmin(role))
        {
            return true;
        }

        if (role == "Customer")
        {
            return ticket.CustomerId == userId;
        }

        if (IsAgent(role))
        {
            if (!requireAssignmentForAgent)
            {
                return true;
            }

            if (ticket.AssignedAgentId == userId)
            {
                return true;
            }

            return await context.TicketAssignments
                .AnyAsync(a => a.TicketId == ticket.Id && a.IsActive && a.AgentId == userId, cancellationToken);
        }

        return false;
    }
}
