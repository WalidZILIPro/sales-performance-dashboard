using Microsoft.AspNetCore.Mvc;
using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Contracts.Responses;
using SalesDashboard.Application.Services;

namespace SalesDashboard.Api.Controllers;

/// <summary>
/// Thin by design: bind, delegate to a service, return. Validation runs in a filter, errors in
/// middleware, and all logic lives in the Application layer.
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
public sealed class DashboardController(
    IDashboardSummaryService summary,
    IManagerRankingService ranking,
    ISalesTrendService trend,
    ICatalogAnalyticsService catalog) : ControllerBase
{
    /// <summary>KPI cards with previous-period comparison, best manager and status breakdown.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] PeriodQuery query,
        CancellationToken cancellationToken) =>
        Ok(await summary.GetSummaryAsync(query, cancellationToken));

    /// <summary>Manager leaderboard, ranked by the chosen metric.</summary>
    [HttpGet("managers")]
    public async Task<ActionResult<ManagerRankingDto>> GetManagerRanking(
        [FromQuery] RankingQuery query,
        CancellationToken cancellationToken) =>
        Ok(await ranking.GetRankingAsync(query, cancellationToken));

    /// <summary>Revenue, gross profit and sales count over time (gap-filled).</summary>
    [HttpGet("trend")]
    public async Task<ActionResult<SalesTrendDto>> GetTrend(
        [FromQuery] TrendQuery query,
        CancellationToken cancellationToken) =>
        Ok(await trend.GetTrendAsync(query, cancellationToken));

    /// <summary>Revenue and gross profit per category with revenue share.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<CategoryBreakdownDto>> GetCategories(
        [FromQuery] PeriodQuery query,
        CancellationToken cancellationToken) =>
        Ok(await catalog.GetCategoryBreakdownAsync(query, cancellationToken));

    /// <summary>Best products by gross profit, revenue or units.</summary>
    [HttpGet("products/top")]
    public async Task<ActionResult<TopProductsDto>> GetTopProducts(
        [FromQuery] TopProductsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await catalog.GetTopProductsAsync(query, cancellationToken));
}
