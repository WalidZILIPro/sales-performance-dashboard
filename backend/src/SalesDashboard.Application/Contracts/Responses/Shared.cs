using SalesDashboard.Application.Periods;

namespace SalesDashboard.Application.Contracts.Responses;

/// <summary>Echoes the period that was analysed, so the client never has to re-derive it.</summary>
public sealed record PeriodDto(
    PeriodPreset Preset,
    DateOnly From,
    DateOnly To,
    DateOnly PreviousFrom,
    DateOnly PreviousTo,
    int Days);

/// <summary>
/// A value next to its previous-period value. <c>ChangePct</c> is a fraction (0.12 = +12%) and is
/// <c>null</c> when the previous value is zero or missing. <c>Change</c> is the absolute difference
/// (for margin: the difference in percentage points, as a fraction).
/// </summary>
public sealed record MetricComparisonDto(decimal? Current, decimal? Previous, decimal? Change, decimal? ChangePct);

public sealed record ManagerRefDto(int Id, string Name, string Initials, string AvatarColor, string Team, string Title);
