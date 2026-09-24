using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace SalesDashboard.Api.Extensions;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Burst size: one dashboard load is 6 requests, so this allows ~10 rapid period switches.</summary>
    public int TokenLimit { get; set; } = 60;

    /// <summary>Sustained rate per client, in requests per second.</summary>
    public int TokensPerSecond { get; set; } = 10;
}

/// <summary>
/// Every endpoint runs aggregate queries over the sales table, so an unthrottled client (a script, a
/// stuck retry loop) can load the database for everyone. A token bucket per client IP allows short
/// bursts, caps the sustained rate, and answers 429 with Retry-After, which the frontend honours.
/// Built into ASP.NET Core 8, so this needs no extra dependency.
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new();

        // Behind the nginx proxy every request comes from nginx's IP. Trust X-Forwarded-For only from
        // private (container) networks, so clients get their own bucket and cannot spoof one from outside.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
        });

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = settings.TokenLimit,
                        TokensPerPeriod = settings.TokensPerSecond,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = { Title = "Too many requests.", Status = StatusCodes.Status429TooManyRequests },
                });
            };
        });

        return services;
    }
}
