using MediatR;
using Microsoft.AspNetCore.Identity;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;

namespace SupportTicketingPlatform.Application.Queries.Auth.Login
{
    // 1. The Query (Input Data)
    public record LoginQuery(string Email, string Password) : IRequest<Result<string>>;

    // 2. The Handler (Business Logic)
    public class LoginQueryHandler : IRequestHandler<LoginQuery, Result<string>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;

        public LoginQueryHandler(UserManager<ApplicationUser> userManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        public async Task<Result<string>> Handle(LoginQuery request, CancellationToken cancellationToken)
        {
            // 1. Find the user
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Result<string>.Failure("Invalid email or password.", ErrorType.Validation);
            }

            // 2. Check password
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                return Result<string>.Failure("Invalid email or password.", ErrorType.Validation);
            }

            // 3. Get user roles (we assume they have one main role like "Customer" or "Agent")
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "Customer";

            // 4. Generate Token
            var token = _tokenService.GenerateToken(user, primaryRole);

            return Result<string>.Success(token);
        }
    }
}
