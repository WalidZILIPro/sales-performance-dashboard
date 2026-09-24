using SalesDashboard.Domain.Common;

namespace SalesDashboard.Application.Periods;

/// <summary>The period being analysed and the previous comparable period it is compared with.</summary>
public sealed record ResolvedPeriod(PeriodPreset Preset, DateRange Current, DateRange Previous);
