import type {
  CategoryBreakdown,
  DashboardSummary,
  ManagerRanking,
  ManagerRankingRow,
  ManagerRef,
  PeriodDto,
  PeriodPreset,
  RankingMetric,
  RecentSales,
  SalesTrend,
  TopProducts,
} from '../api/types';

export const period = (url: URL): PeriodDto => ({
  preset: (url.searchParams.get('preset') ?? 'Last30Days') as PeriodPreset,
  from: '2026-08-26',
  to: '2026-09-24',
  previousFrom: '2026-07-27',
  previousTo: '2026-08-25',
  days: 30,
});

const smirnov: ManagerRef = { id: 1, name: 'Алексей Смирнов', initials: 'АС', avatarColor: '#4F46E5', team: 'Enterprise', title: 'Head of Sales' };
const ivanova: ManagerRef = { id: 2, name: 'Мария Иванова', initials: 'МИ', avatarColor: '#0EA5E9', team: 'Enterprise', title: 'Senior Sales Manager' };
const petrova: ManagerRef = { id: 3, name: 'Татьяна Петрова', initials: 'ТП', avatarColor: '#10B981', team: 'SMB', title: 'Junior Sales Manager' };

export const summary = (url: URL): DashboardSummary => ({
  period: period(url),
  kpis: {
    revenue: { current: 161_400_000, previous: 154_100_000, change: 7_300_000, changePct: 0.0474 },
    grossProfit: { current: 25_000_000, previous: 24_000_000, change: 1_000_000, changePct: 0.0417 },
    margin: { current: 0.1568, previous: 0.1561, change: 0.0007, changePct: 0.0045 },
    salesCount: { current: 342, previous: 262, change: 80, changePct: 0.3053 },
    averageCheck: { current: 473_540.64, previous: 590_214.03, change: -116_673.39, changePct: -0.1977 },
  },
  bestManager: { manager: smirnov, grossProfit: 5_600_000, revenue: 29_000_000, salesCount: 36, grossProfitChangePct: 0.068 },
  statuses: {
    paidCount: 342,
    cancelledCount: 17,
    refundedCount: 16,
    cancelledAmount: 13_534_449,
    refundedAmount: 4_158_776,
    cancellationRate: 0.0453,
    refundRate: 0.0427,
  },
});

export const emptySummary = (url: URL): DashboardSummary => {
  const zero = { current: 0, previous: 0, change: 0, changePct: null };
  const none = { current: null, previous: null, change: null, changePct: null };
  return {
    period: period(url),
    kpis: { revenue: zero, grossProfit: zero, margin: none, salesCount: zero, averageCheck: none },
    bestManager: null,
    statuses: { paidCount: 0, cancelledCount: 0, refundedCount: 0, cancelledAmount: 0, refundedAmount: 0, cancellationRate: null, refundRate: null },
  };
};

export const trend = (url: URL): SalesTrend => ({
  period: period(url),
  granularity: 'Day',
  points: [
    { bucketStart: '2026-09-22', revenue: 5_000_000, grossProfit: 800_000, salesCount: 12 },
    { bucketStart: '2026-09-23', revenue: 7_000_000, grossProfit: 1_100_000, salesCount: 15 },
    { bucketStart: '2026-09-24', revenue: 4_000_000, grossProfit: 600_000, salesCount: 9 },
  ],
});

const row = (manager: ManagerRef, rank: number | null, gp: number, avg: number | null): ManagerRankingRow => ({
  rank,
  previousRank: rank,
  rankChange: rank === null ? null : 0,
  manager,
  hasSales: rank !== null,
  metricValue: gp,
  metricChangePct: 0.05,
  salesCount: rank === null ? 0 : 10,
  revenue: gp * 6,
  grossProfit: gp,
  averageCheck: avg,
  margin: rank === null ? null : 0.16,
});

/** Like the real API: the order depends on the metric. Smirnov leads on profit, Ivanova on average check. */
export const managers = (url: URL): ManagerRanking => {
  const rankBy = (url.searchParams.get('rankBy') ?? 'GrossProfit') as RankingMetric;
  const byProfit = [row(smirnov, 1, 5_600_000, 400_000), row(ivanova, 2, 3_500_000, 900_000)];
  const byCheck = [row(ivanova, 1, 3_500_000, 900_000), row(smirnov, 2, 5_600_000, 400_000)];
  return {
    period: period(url),
    rankBy,
    managers: [...(rankBy === 'AverageCheck' ? byCheck : byProfit), row(petrova, null, 0, null)],
  };
};

export const categories = (url: URL): CategoryBreakdown => ({
  period: period(url),
  totalRevenue: 161_950_900,
  categories: [
    { categoryId: 6, name: 'Enterprise Solutions', revenue: 98_385_050, grossProfit: 16_491_951, margin: 0.1676, units: 119, revenueShare: 0.61 },
    { categoryId: 8, name: 'Accessories & Parts', revenue: 769_110, grossProfit: 338_991, margin: 0.4408, units: 271, revenueShare: 0.005 },
  ],
});

export const products = (url: URL): TopProducts => ({
  period: period(url),
  sortBy: 'GrossProfit',
  products: [
    { productId: 1, sku: 'DJI-ES-005', name: 'DJI Agras T50', category: 'Enterprise Solutions', revenue: 40_000_000, grossProfit: 6_500_000, margin: 0.16, units: 25 },
  ],
});

export const sales = (url: URL): RecentSales => ({
  period: period(url),
  sales: {
    items: [
      {
        id: 4802,
        number: 'S-004802',
        soldAt: '2026-09-24T11:40:00+00:00',
        status: 'Refunded',
        manager: ivanova,
        customer: { id: 25, name: 'Марина Фёдорова', company: 'ООО «МеридианМедиа»' },
        items: [{ productName: 'Propellers (Mini 4 Pro)', quantity: 6 }],
        amount: 5_580,
        grossProfit: 3_348,
      },
    ],
    page: Number(url.searchParams.get('page') ?? 1),
    pageSize: 10,
    totalCount: 1,
    totalPages: 1,
  },
});
