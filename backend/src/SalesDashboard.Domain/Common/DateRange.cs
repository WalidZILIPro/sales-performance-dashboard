namespace SalesDashboard.Domain.Common;

/// <summary>
/// An inclusive range of whole UTC calendar days. Converted to a half-open instant interval
/// [<see cref="StartUtc"/>, <see cref="EndExclusiveUtc"/>) for querying, so a sale at exactly
/// 00:00:00 on <see cref="From"/> is included and one at 00:00:00 the day after <see cref="To"/> is not.
/// </summary>
public readonly record struct DateRange
{
    public DateRange(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new ArgumentException("'to' must not be earlier than 'from'.", nameof(to));
        }

        From = from;
        To = to;
    }

    public DateOnly From { get; }

    public DateOnly To { get; }

    public int Days => To.DayNumber - From.DayNumber + 1;

    public DateTimeOffset StartUtc => ToUtcMidnight(From);

    public DateTimeOffset EndExclusiveUtc => ToUtcMidnight(To.AddDays(1));

    public bool Contains(DateTimeOffset instant) => instant >= StartUtc && instant < EndExclusiveUtc;

    private static DateTimeOffset ToUtcMidnight(DateOnly date) =>
        new(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
}
