using Microsoft.Extensions.DependencyInjection;

namespace SupportTicketingPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => { }, typeof(DependencyInjection).Assembly);

        return services;
    }
}
