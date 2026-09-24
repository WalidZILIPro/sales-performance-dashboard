using SalesDashboard.Domain.Enums;

namespace SalesDashboard.Domain.Rules;

/// <summary>
/// The single place that decides which sales count towards the money metrics.
///
/// Rule: only <see cref="SaleStatus.Paid"/> sales contribute to Revenue, Gross Profit, Margin,
/// Average Check and the sales count. A Refunded sale is treated as fully reversed (the money went
/// back and the goods returned to stock, so cost is reversed too); a Cancelled sale never happened
/// financially. Both are still reported separately as a status breakdown.
/// </summary>
public static class SalesRules
{
    public const SaleStatus RevenueStatus = SaleStatus.Paid;

    public static bool CountsTowardRevenue(SaleStatus status) => status == RevenueStatus;
}
