using MediatR;
using Microsoft.AspNetCore.Identity;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Domain.Entities;

namespace SupportTicketingPlatform.Application.Commands.Auth.RegisterCustomer
{
    // 1. The Command (Input Data)
    public record RegisterCustomerCommand(string Email, string Password, string DisplayName, string Phone) : IRequest<Result<string>>;

    // 2. The Handler (Business Logic)
    public class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, Result<string>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterCustomerCommandHandler(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<string>> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
        {
            // Create user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                CustomerProfile = new CustomerProfile
                {
                    DisplayName = request.DisplayName,
                    Phone = request.Phone
                }
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<string>.Failure(errors, ErrorType.Validation);
            }

            // Ensure "Customer" role exists and assign it
            if (!await _roleManager.RoleExistsAsync("Customer"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Customer"));
            }
            await _userManager.AddToRoleAsync(user, "Customer");

            return Result<string>.Success(user.Id);
        }
    }
}
