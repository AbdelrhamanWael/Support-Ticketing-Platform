using Serilog;
using Scalar.AspNetCore;
using SupportTicketingPlatform.Application;
using SupportTicketingPlatform.Infrastructure;
using Microsoft.AspNetCore.OpenApi;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Support Ticketing Platform API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var app = builder.Build();
    
    app.UseMiddleware<SupportTicketingPlatform.API.Middleware.ExceptionHandlingMiddleware>();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithPreferredScheme("Bearer")
                   .WithHttpBearerAuthentication(bearer =>
                   {
                       bearer.Token = "REPLACE_WITH_YOUR_TOKEN";
                   });
        });
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
