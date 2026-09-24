// Mirrors the backend DTOs (SalesDashboard.Application/Contracts). Enums travel as strings.
// Ratios (margin, shares, changePct) are fractions: 0.12 = 12%. Dates are yyyy-MM-dd (UTC days).

export const PRESETS = ['Today', 'Last7Days', 'Last30Days', 'ThisMonth', 'LastMonth', 'Custom'] as const;
export type PeriodPreset = (typeof PRESETS)[number];

export const RANKING_METRICS = ['GrossProfit', 'AverageCheck', 'Revenue', 'Margin'] as const;
export type RankingMetric = (typeof RANKING_METRICS)[number];

export const PRODUCT_SORTS = ['GrossProfit', 'Revenue', 'Units'] as const;
export type ProductSortBy = (typeof PRODUCT_SORTS)[number];

export const SALE_STATUSES = ['Paid', 'Cancelled', 'Refunded'] as const;
export type SaleStatus = (typeof SALE_STATUSES)[number];

export type BucketSize = 'Day' | 'Week' | 'Month';

export interface PeriodDto {
  preset: PeriodPreset;
  from: string;
  to: string;
  previousFrom: string;
  previousTo: string;
  days: number;
}

export interface MetricComparison {
  current: number | null;
  previous: number | null;
  change: number | null;
  changePct: number | null;
}

export interface ManagerRef {
  id: number;
  name: string;
  initials: string;
  avatarColor: string;
  team: string;
  title: string;
}

export interface DashboardSummary {
  period: PeriodDto;
  kpis: {
    revenue: MetricComparison;
    grossProfit: MetricComparison;
    margin: MetricComparison;
    salesCount: MetricComparison;
    averageCheck: MetricComparison;
  };
  bestManager: {
    manager: ManagerRef;
    grossProfit: number;
    revenue: number;
    salesCount: number;
    grossProfitChangePct: number | null;
  } | null;
  statuses: {
    paidCount: number;
    cancelledCount: number;
    refundedCount: number;
    cancelledAmount: number;
    refundedAmount: number;
    cancellationRate: number | null;
    refundRate: number | null;
  };
}

export interface ManagerRankingRow {
  /** null for managers without sales: listed last, not ranked. Ties share a rank (1, 1, 3). */
  rank: number | null;
  previousRank: number | null;
  /** previousRank - rank: positive = moved up. */
  rankChange: number | null;
  manager: ManagerRef;
  hasSales: boolean;
  metricValue: number | null;
  metricChangePct: number | null;
  salesCount: number;
  revenue: number;
  grossProfit: number;
  averageCheck: number | null;
  margin: number | null;
}

export interface ManagerRanking {
  period: PeriodDto;
  rankBy: RankingMetric;
  managers: ManagerRankingRow[];
}

export interface TrendPoint {
  bucketStart: string;
  revenue: number;
  grossProfit: number;
  salesCount: number;
}

export interface SalesTrend {
  period: PeriodDto;
  granularity: BucketSize;
  /** Gap-filled: a bucket without sales is present with zeros. */
  points: TrendPoint[];
}

export interface CategoryPerformance {
  categoryId: number;
  name: string;
  revenue: number;
  grossProfit: number;
  margin: number | null;
  units: number;
  revenueShare: number | null;
}

export interface CategoryBreakdown {
  period: PeriodDto;
  totalRevenue: number;
  categories: CategoryPerformance[];
}

export interface ProductPerformance {
  productId: number;
  sku: string;
  name: string;
  category: string;
  revenue: number;
  grossProfit: number;
  margin: number | null;
  units: number;
}

export interface TopProducts {
  period: PeriodDto;
  sortBy: ProductSortBy;
  products: ProductPerformance[];
}

export interface RecentSale {
  id: number;
  number: string;
  soldAt: string;
  status: SaleStatus;
  manager: ManagerRef;
  customer: { id: number; name: string; company: string };
  items: { productName: string; quantity: number }[];
  amount: number;
  grossProfit: number;
}

export interface RecentSales {
  period: PeriodDto;
  sales: {
    items: RecentSale[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

/** The period part of every request. `from`/`to` only with `Custom`. */
export interface PeriodParams {
  preset: PeriodPreset;
  from?: string;
  to?: string;
}
