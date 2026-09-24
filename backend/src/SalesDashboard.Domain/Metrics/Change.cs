namespace SalesDashboard.Domain.Metrics;

public static class Change
{
    /// <summary>
    /// Relative change (current - previous) / |previous| as a fraction (0.12 = +12%).
    /// <c>null</c> when it is undefined: either side missing, or the previous value is zero.
    /// </summary>
    public static decimal? Relative(decimal? current, decimal? previous)
    {
        if (current is null || previous is null || previous == 0m)
        {
            return null;
        }

        return (current.Value - previous.Value) / Math.Abs(previous.Value);
    }

    /// <summary>Absolute difference, <c>null</c> when either side is missing.</summary>
    public static decimal? Absolute(decimal? current, decimal? previous) =>
        current is null || previous is null ? null : current.Value - previous.Value;
}
