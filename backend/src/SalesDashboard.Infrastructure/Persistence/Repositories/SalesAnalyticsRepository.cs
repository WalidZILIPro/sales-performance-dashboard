using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Persistence;
using SalesDashboard.Application.Persistence.Models;
using SalesDashboard.Domain.Common;
using SalesDashboard.Domain.Entities;
using SalesDashboard.Domain.Enums;
using SalesDashboard.Domain.Metrics;
using SalesDashboard.Domain.Rules;

namespace SalesDashboard.Infrastructure.Persistence.Repositories;

internal sealed class SalesAnalyticsRepository(AppDbContext db) : ISalesAnalyticsRepository
{
    public async Task<IReadOnlyList<ManagerPerformance>> GetManagerPerformanceAsync(
        DateRange current,
        DateRange previous,
        CancellationToken cancellationToken)
    {
        var curStart = current.StartUtc;
        var curEnd = current.EndExclusiveUtc;
        var prevStart = previous.StartUtc;
        var prevEnd = previous.EndExclusiveUtc;
        const SaleStatus paid = SalesRules.RevenueStatus;

        // One scan, one GROUP BY: both periods are computed with conditional aggregates
        // (SUM(...) FILTER (WHERE ...)) instead of running the same query twice.
        var aggregates = await db.Sales
            .Where(s => s.Status == paid
                && ((s.SoldAt >= curStart && s.SoldAt < curEnd) || (s.SoldAt >= prevStart && s.SoldAt < prevEnd)))
            .GroupBy(s => s.ManagerId)
            .Select(g => new
            {
                ManagerId = g.Key,
                CurrentRevenue = g.Where(s => s.SoldAt >= curStart && s.SoldAt < curEnd).Sum(s => s.TotalAmount),
                CurrentCost = g.Where(s => s.SoldAt >= curStart && s.SoldAt < curEnd).Sum(s => s.TotalCost),
                CurrentCount = g.Count(s => s.SoldAt >= curStart && s.SoldAt < curEnd),
                PreviousRevenue = g.Where(s => s.SoldAt >= prevStart && s.SoldAt < prevEnd).Sum(s => s.TotalAmount),
                PreviousCost = g.Where(s => s.SoldAt >= prevStart && s.SoldAt < prevEnd).Sum(s => s.TotalCost),
                PreviousCount = g.Count(s => s.SoldAt >= prevStart && s.SoldAt < prevEnd),
            })
            .ToListAsync(cancellationToken);

        var soldIds = aggregates.Select(a => a.ManagerId).ToList();

        // The manager list is tiny (tens of rows), so a second lookup beats a join that would
        // repeat every manager column on each aggregate row.
        var managers = await db.Managers
            .Where(m => m.IsActive || soldIds.Contains(m.Id))
            .Select(m => new { m.Id, m.FirstName, m.LastName, m.AvatarColor, m.Team, m.Title, m.IsActive })
            .ToListAsync(cancellationToken);

        var byManager = aggregates.ToDictionary(a => a.ManagerId);

        return managers
            .Select(m =>
            {
                var info = new ManagerInfo(
                    m.Id,
                    $"{m.FirstName} {m.LastName}",
                    Manager.BuildInitials(m.FirstName, m.LastName),
                    m.AvatarColor,
                    m.Team,
                    m.Title,
                    m.IsActive);

                return byManager.TryGetValue(m.Id, out var a)
                    ? new ManagerPerformance(
                        info,
                        new SalesMetrics(a.CurrentRevenue, a.CurrentCost, a.CurrentCount),
                        new SalesMetrics(a.PreviousRevenue, a.PreviousCost, a.PreviousCount))
                    : new ManagerPerformance(info, SalesMetrics.Empty, SalesMetrics.Empty);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<StatusStatistic>> GetStatusStatisticsAsync(
        DateRange range,
        CancellationToken cancellationToken)
    {
        var start = range.StartUtc;
        var end = range.EndExclusiveUtc;

        var rows = await db.Sales
            .Where(s => s.SoldAt >= start && s.SoldAt < end)
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(s => s.TotalAmount) })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new StatusStatistic(r.Status, r.Count, r.Amount)).ToList();
    }

    // Raw SQL, deliberately: bucketing needs date_trunc(), which LINQ cannot express for timestamptz
    // in a way that stays index-friendly and readable. The unit is not user text: it comes from the
    // BucketSize enum, and every value is still passed as a parameter.
    public async Task<IReadOnlyList<TrendBucket>> GetTrendAsync(
        DateRange range,
        BucketSize bucketSize,
        CancellationToken cancellationToken)
    {
        var unit = bucketSize switch
        {
            BucketSize.Day => "day",
            BucketSize.Week => "week",
            BucketSize.Month => "month",
            _ => throw new ArgumentOutOfRangeException(nameof(bucketSize), bucketSize, null),
        };
        var paid = SalesRules.RevenueStatus.ToString();
        var start = range.StartUtc;
        var end = range.EndExclusiveUtc;

        var rows = await db.Database
            .SqlQuery<TrendRow>($"""
                SELECT date_trunc({unit}, sold_at AT TIME ZONE 'UTC')::date AS bucket_start,
                       SUM(total_amount)                                     AS revenue,
                       SUM(total_cost)                                       AS cost,
                       COUNT(*)::int                                         AS sales_count
                FROM sales
                WHERE status = {paid} AND sold_at >= {start} AND sold_at < {end}
                GROUP BY 1
                ORDER BY 1
                """)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new TrendBucket(r.BucketStart, new SalesMetrics(r.Revenue, r.Cost, r.SalesCount))).ToList();
    }

    private sealed class TrendRow
    {
        [Column("bucket_start")]
        public DateOnly BucketStart { get; set; }

        [Column("revenue")]
        public decimal Revenue { get; set; }

        [Column("cost")]
        public decimal Cost { get; set; }

        [Column("sales_count")]
        public int SalesCount { get; set; }
    }
}
