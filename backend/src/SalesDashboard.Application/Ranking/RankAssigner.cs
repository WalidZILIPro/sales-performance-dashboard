namespace SalesDashboard.Application.Ranking;

/// <summary>
/// Standard competition ranking ("1224"): equal values share a rank and the next rank skips ahead,
/// so two managers with the same Gross Profit are both #1 and the next one is #3.
/// Items whose value is <c>null</c> are not ranked.
/// </summary>
internal static class RankAssigner
{
    public static Dictionary<int, int> Assign<T>(
        IEnumerable<T> items,
        Func<T, int> idSelector,
        Func<T, decimal?> valueSelector)
    {
        var ordered = items
            .Select(item => (Id: idSelector(item), Value: valueSelector(item)))
            .Where(x => x.Value.HasValue)
            .OrderByDescending(x => x.Value)
            .ToList();

        var ranks = new Dictionary<int, int>(ordered.Count);
        var currentRank = 0;
        decimal? previousValue = null;

        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].Value != previousValue)
            {
                currentRank = i + 1;
                previousValue = ordered[i].Value;
            }

            ranks[ordered[i].Id] = currentRank;
        }

        return ranks;
    }
}
