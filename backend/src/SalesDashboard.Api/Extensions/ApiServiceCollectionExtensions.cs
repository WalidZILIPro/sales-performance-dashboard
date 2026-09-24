using System.Text.Json;
using System.Text.Json.Serialization;
using SalesDashboard.Api.Filters;
using SalesDashboard.Api.Middleware;
using SalesDashboard.Infrastructure.Persistence;

namespace SalesDashboard.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public const string CorsPolicy = "Frontend";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers(options => options.Filters.Add<ValidationFilter>())
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            });

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("postgres");
        services.AddResponseCompression();
        services.AddSwagger();
        services.AddApiRateLimiting(configuration);

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(origins)
            .WithMethods("GET")
            .AllowAnyHeader()));

        return services;
    }
}
