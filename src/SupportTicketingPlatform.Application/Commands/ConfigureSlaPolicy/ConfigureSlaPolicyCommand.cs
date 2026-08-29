using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Commands.ConfigureSlaPolicy;

public record ConfigureSlaPolicyCommand(
    int? CategoryId,
    TicketPriority Priority,
    int ResponseTargetMinutes,
    int ResolutionTargetMinutes) : IRequest<Result<SlaPolicyDto>>;

public record SlaPolicyDto(
    int Id,
    string? Category,
    TicketPriority Priority,
    int ResponseTargetMinutes,
    int ResolutionTargetMinutes,
    bool IsActive);

public class ConfigureSlaPolicyCommandHandler : IRequestHandler<ConfigureSlaPolicyCommand, Result<SlaPolicyDto>>
{
    private readonly IAppDbContext _context;

    public ConfigureSlaPolicyCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SlaPolicyDto>> Handle(ConfigureSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        if (request.ResponseTargetMinutes <= 0 || request.ResolutionTargetMinutes <= 0)
        {
            return Result<SlaPolicyDto>.Failure(
                "Response and resolution targets must be greater than zero.",
                ErrorType.Validation);
        }

        if (request.ResponseTargetMinutes >= request.ResolutionTargetMinutes)
        {
            return Result<SlaPolicyDto>.Failure(
                "Response target must be less than resolution target.",
                ErrorType.Validation);
        }

        string? categoryName = null;
        if (request.CategoryId.HasValue)
        {
            var category = await _context.TicketCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value && c.IsActive, cancellationToken);

            if (category is null)
            {
                return Result<SlaPolicyDto>.Failure(
                    $"Category with ID {request.CategoryId.Value} was not found or is inactive.",
                    ErrorType.NotFound);
            }

            categoryName = category.Name;
        }

        var existing = await _context.SlaPolicies
            .FirstOrDefaultAsync(
                p => p.Priority == request.Priority && p.TicketCategoryId == request.CategoryId,
                cancellationToken);

        if (existing is null)
        {
            existing = new SlaPolicy
            {
                TicketCategoryId = request.CategoryId,
                Priority = request.Priority,
                ResponseTargetMinutes = request.ResponseTargetMinutes,
                ResolutionTargetMinutes = request.ResolutionTargetMinutes,
                IsActive = true
            };

            _context.SlaPolicies.Add(existing);
        }
        else
        {
            existing.ResponseTargetMinutes = request.ResponseTargetMinutes;
            existing.ResolutionTargetMinutes = request.ResolutionTargetMinutes;
            existing.IsActive = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<SlaPolicyDto>.Success(new SlaPolicyDto(
            existing.Id,
            categoryName,
            existing.Priority,
            existing.ResponseTargetMinutes,
            existing.ResolutionTargetMinutes,
            existing.IsActive));
    }
}
