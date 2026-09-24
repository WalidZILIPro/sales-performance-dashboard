using Microsoft.OpenApi.Models;

namespace SalesDashboard.Api.Extensions;

public static class SwaggerExtensions
{
    private static readonly string[] XmlDocFiles = ["SalesDashboard.Api.xml", "SalesDashboard.Application.xml"];

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Sales Performance Dashboard API",
                Version = "v1",
                Description = """
                    Read-only analytics over sales for the DJI-Market.ru dashboard. All figures are computed
                    on the server.

                    **Periods.** Every endpoint takes `preset` (Today, Last7Days, Last30Days, ThisMonth,
                    LastMonth, Custom). `from` and `to` (yyyy-MM-dd, inclusive) are allowed only with `Custom`.
                    All periods are whole UTC days. The response echoes the resolved current and previous period.

                    **Business rules.** Only `Paid` sales count towards Revenue, Gross Profit, Margin, Average
                    Check and sales count. `Cancelled` and `Refunded` sales are excluded and reported separately.
                    Ratios (margin, shares, change) are fractions: 0.12 means 12%. A ratio is `null` when it is
                    undefined, for example a change against a previous value of zero.

                    **Errors.** Every error is `application/problem+json` (RFC 7807): 400 for invalid input with
                    errors keyed by field, 500 with a `traceId` for anything unexpected.
                    """,
            });

            options.SupportNonNullableReferenceTypes();

            // Controller summaries (Api) and DTO / query field docs (Application).
            foreach (var file in XmlDocFiles)
            {
                var path = Path.Combine(AppContext.BaseDirectory, file);
                if (File.Exists(path))
                {
                    options.IncludeXmlComments(path, includeControllerXmlComments: true);
                }
            }
        });

        return services;
    }

    public static WebApplication UseSwaggerDocs(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sales Performance Dashboard API v1");
            options.DocumentTitle = "Sales Dashboard API";
            options.DisplayRequestDuration();
            options.EnableTryItOutByDefault();
            options.DefaultModelsExpandDepth(-1); // schemas are inline in each response; hide the long list
        });

        // Opening the bare API address lands on the docs instead of a 404.
        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

        return app;
    }
}
