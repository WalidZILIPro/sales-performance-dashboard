using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Contracts.Responses;
using SalesDashboard.Application.Persistence.Models;
using SalesDashboard.Domain.Common;
using SalesDashboard.Domain.Metrics;

namespace SalesDashboard.Application.Trends;

/// <summary>Pure functions: pick a bucket size and turn sparse database buckets into a continuous series.</summary>
public static class TrendBucketing
{
    private const int MaxDaysForDailyBuckets = 45;
    private const int MaxDaysForWeeklyBuckets = 200;

    public static BucketSize Choose(int days) => days switch
    {
        <= MaxDaysForDailyBuckets => BucketSize.Day,
        <= MaxDaysForWeeklyBuckets => BucketSize.Week,
        _ => BucketSize.Month,
    };

    /// <summary>First day of the bucket containing <paramref name="date"/> (ISO week: Monday).</summary>
    public static DateOnly BucketStart(DateOnly date, BucketSize size) => size switch
    {
        BucketSize.Day => date,
        BucketSize.Week => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
        BucketSize.Month => new DateOnly(date.Year, date.Month, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    public static IReadOnlyList<TrendPointDto> FillGaps(
        DateRange range,
        BucketSize size,
        IEnumerable<TrendBucket> buckets)
    {
        var byStart = buckets.ToDictionary(b => b.BucketStart, b => b.Metrics);
        var points = new List<TrendPointDto>();

        for (var start = BucketStart(range.From, size); start <= range.To; start = Next(start, size))
        {
            var metrics = byStart.GetValueOrDefault(start, SalesMetrics.Empty);
            points.Add(new TrendPointDto(
                start,
                Math.Round(metrics.Revenue, 2, MidpointRounding.AwayFromZero),
                Math.Round(metrics.GrossProfit, 2, MidpointRounding.AwayFromZero),
                metrics.SalesCount));
        }

        return points;
    }

    private static DateOnly Next(DateOnly start, BucketSize size) => size switch
    {
        BucketSize.Day => start.AddDays(1),
        BucketSize.Week => start.AddDays(7),
        BucketSize.Month => start.AddMonths(1),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };
}
