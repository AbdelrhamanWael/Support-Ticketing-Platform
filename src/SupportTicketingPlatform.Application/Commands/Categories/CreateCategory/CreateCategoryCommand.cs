using MediatR;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;

namespace SupportTicketingPlatform.Application.Commands.Categories.CreateCategory
{
    public record CreateCategoryCommand(string Name, string Description) : IRequest<Result<int>>;

    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<int>>
    {
        private readonly IAppDbContext _context;

        public CreateCategoryCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<int>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = new TicketCategory
            {
                Name = request.Name,
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.TicketCategories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<int>.Success(category.Id);
        }
    }
}
