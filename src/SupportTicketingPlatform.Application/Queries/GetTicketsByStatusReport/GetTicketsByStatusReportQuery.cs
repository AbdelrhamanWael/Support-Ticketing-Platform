using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.DTOs;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Queries.GetTicketsByStatusReport;

public record GetTicketsByStatusReportQuery(DateTime? DateFrom = null, DateTime? DateTo = null)
    : IRequest<Result<TicketsByStatusReportDto>>;

public class GetTicketsByStatusReportQueryHandler
    : IRequestHandler<GetTicketsByStatusReportQuery, Result<TicketsByStatusReportDto>>
{
    private readonly IAppDbContext _context;

    public GetTicketsByStatusReportQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<TicketsByStatusReportDto>> Handle(
        GetTicketsByStatusReportQuery request,
        CancellationToken cancellationToken)
    {
        var from = request.DateFrom ?? DateTime.UtcNow.AddDays(-30);
        var to = request.DateTo ?? DateTime.UtcNow;

        var tickets = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Status != TicketStatus.Cancelled)
            .Where(t => t.CreatedAt >= from && t.CreatedAt <= to)
            .Select(t => new { t.Status, t.Priority })
            .ToListAsync(cancellationToken);

        var byStatus = tickets
            .GroupBy(t => t.Status)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        var byPriority = tickets
            .GroupBy(t => t.Priority)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        return Result<TicketsByStatusReportDto>.Success(new TicketsByStatusReportDto
        {
            DateRange = new DateRangeDto { From = from, To = to },
            ByStatus = byStatus,
            ByPriority = byPriority
        });
    }
}
