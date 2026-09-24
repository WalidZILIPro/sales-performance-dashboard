using Microsoft.AspNetCore.Mvc;
using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Contracts.Responses;
using SalesDashboard.Application.Services;

namespace SalesDashboard.Api.Controllers;

[ApiController]
[Route("api/v1/sales")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
public sealed class SalesController(IRecentSalesService recentSales) : ControllerBase
{
    /// <summary>Paged list of sales in the period, newest first, of any status unless filtered.</summary>
    [HttpGet]
    public async Task<ActionResult<RecentSalesDto>> GetRecentSales(
        [FromQuery] RecentSalesQuery query,
        CancellationToken cancellationToken) =>
        Ok(await recentSales.GetRecentSalesAsync(query, cancellationToken));
}
