using SalesDashboard.Api.Extensions;
using SalesDashboard.Application;
using SalesDashboard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseResponseCompression();
app.UseCors(ApiServiceCollectionExtensions.CorsPolicy);
app.UseRateLimiter();

app.UseSwaggerDocs();

app.MapControllers();
// Health probes must never be throttled, or an orchestrator would restart a healthy container.
app.MapHealthChecks("/health").DisableRateLimiting();

app.Run();

// Exposed so integration tests can host the API with WebApplicationFactory<Program>.
public partial class Program;
