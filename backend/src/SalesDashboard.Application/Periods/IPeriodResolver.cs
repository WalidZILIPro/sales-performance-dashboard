using SalesDashboard.Application.Contracts.Requests;

namespace SalesDashboard.Application.Periods;

public interface IPeriodResolver
{
    ResolvedPeriod Resolve(PeriodQuery query);
}
