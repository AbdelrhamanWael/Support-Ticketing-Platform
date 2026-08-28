using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Common;

public static class SlaCalculationHelper
{
    public static SlaStatus DeriveStatus(Ticket ticket, DateTime now)
    {
        if (ticket.SlaResolutionDueAt is null)
        {
            return SlaStatus.OnTrack;
        }

        if (now >= ticket.SlaResolutionDueAt)
        {
            return SlaStatus.Breached;
        }

        var totalWindowMinutes = (ticket.SlaResolutionDueAt.Value - ticket.CreatedAt).TotalMinutes;
        if (totalWindowMinutes <= 0)
        {
            return SlaStatus.OnTrack;
        }

        var atRiskThreshold = ticket.SlaResolutionDueAt.Value.AddMinutes(-totalWindowMinutes * 0.30);
        if (now >= atRiskThreshold)
        {
            return SlaStatus.AtRisk;
        }

        return SlaStatus.OnTrack;
    }

    public static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;

        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }
}
