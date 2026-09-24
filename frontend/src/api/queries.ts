import { keepPreviousData, QueryClient, useQuery } from '@tanstack/react-query';
import { apiGet, ApiError } from './client';
import type {
  CategoryBreakdown,
  DashboardSummary,
  ManagerRanking,
  PeriodParams,
  ProductSortBy,
  RankingMetric,
  RecentSales,
  SaleStatus,
  SalesTrend,
  TopProducts,
} from './types';

export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // Past periods never change; a minute keeps "Today" fresh enough without refetch storms.
        staleTime: 60_000,
        refetchOnWindowFocus: false,
        // Retry what can recover (network, 5xx, 429). A 400 will fail the same way every time.
        retry: (failureCount, error) => error instanceof ApiError && error.isTransient && failureCount < 2,
        // Honour the server's Retry-After on a 429; otherwise back off exponentially.
        retryDelay: (attempt, error) =>
          error instanceof ApiError && error.retryAfter !== null
            ? error.retryAfter * 1000
            : Math.min(1000 * 2 ** attempt, 8000),
        // On a period change the old numbers stay on screen (dimmed) until the new ones arrive,
        // instead of the whole page flashing back to skeletons.
        placeholderData: keepPreviousData,
      },
    },
  });
}

const periodKey = (p: PeriodParams) => [p.preset, p.from ?? '', p.to ?? ''] as const;

export function useSummary(period: PeriodParams) {
  return useQuery({
    queryKey: ['summary', ...periodKey(period)],
    queryFn: ({ signal }) => apiGet<DashboardSummary>('/dashboard/summary', { ...period }, signal),
  });
}

export function useTrend(period: PeriodParams) {
  return useQuery({
    queryKey: ['trend', ...periodKey(period)],
    queryFn: ({ signal }) => apiGet<SalesTrend>('/dashboard/trend', { ...period }, signal),
  });
}

export function useManagerRanking(period: PeriodParams, rankBy: RankingMetric) {
  return useQuery({
    queryKey: ['managers', ...periodKey(period), rankBy],
    queryFn: ({ signal }) => apiGet<ManagerRanking>('/dashboard/managers', { ...period, rankBy }, signal),
  });
}

export function useCategories(period: PeriodParams) {
  return useQuery({
    queryKey: ['categories', ...periodKey(period)],
    queryFn: ({ signal }) => apiGet<CategoryBreakdown>('/dashboard/categories', { ...period }, signal),
  });
}

export function useTopProducts(period: PeriodParams, sortBy: ProductSortBy) {
  return useQuery({
    queryKey: ['products', ...periodKey(period), sortBy],
    queryFn: ({ signal }) => apiGet<TopProducts>('/dashboard/products/top', { ...period, sortBy, limit: 8 }, signal),
  });
}

export function useRecentSales(period: PeriodParams, page: number, status: SaleStatus | null) {
  return useQuery({
    queryKey: ['sales', ...periodKey(period), page, status],
    queryFn: ({ signal }) =>
      apiGet<RecentSales>('/sales', { ...period, page, pageSize: 10, status }, signal),
  });
}
