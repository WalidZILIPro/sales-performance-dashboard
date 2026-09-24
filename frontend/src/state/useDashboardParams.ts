import { useCallback, useMemo, useSyncExternalStore } from 'react';
import {
  PRESETS,
  PRODUCT_SORTS,
  RANKING_METRICS,
  SALE_STATUSES,
  type PeriodParams,
  type ProductSortBy,
  type RankingMetric,
  type SaleStatus,
} from '../api/types';

/**
 * All dashboard state lives in the URL (?preset=Last7Days&rankBy=AverageCheck...). That gives
 * shareable links and a working Back button without a router or a global store: the URL is the store.
 */
export interface DashboardParams {
  period: PeriodParams;
  rankBy: RankingMetric;
  productSort: ProductSortBy;
  saleStatus: SaleStatus | null;
  salesPage: number;
}

const DEFAULTS: DashboardParams = {
  period: { preset: 'Last30Days' },
  rankBy: 'GrossProfit',
  productSort: 'GrossProfit',
  saleStatus: null,
  salesPage: 1,
};

const ISO_DAY = /^\d{4}-\d{2}-\d{2}$/;

function oneOf<T extends string>(values: readonly T[], value: string | null, fallback: T): T {
  return values.includes(value as T) ? (value as T) : fallback;
}

export function parseParams(search: string): DashboardParams {
  const q = new URLSearchParams(search);
  const preset = oneOf(PRESETS, q.get('preset'), DEFAULTS.period.preset);
  const from = q.get('from');
  const to = q.get('to');

  // A Custom period without valid dates is meaningless: fall back to the default instead of a 400.
  const period: PeriodParams =
    preset === 'Custom'
      ? from && to && ISO_DAY.test(from) && ISO_DAY.test(to)
        ? { preset, from, to }
        : DEFAULTS.period
      : { preset };

  const page = Number(q.get('page'));
  const status = q.get('status');
  return {
    period,
    rankBy: oneOf(RANKING_METRICS, q.get('rankBy'), DEFAULTS.rankBy),
    productSort: oneOf(PRODUCT_SORTS, q.get('productSort'), DEFAULTS.productSort),
    saleStatus: SALE_STATUSES.includes(status as SaleStatus) ? (status as SaleStatus) : null,
    salesPage: Number.isInteger(page) && page > 0 ? page : DEFAULTS.salesPage,
  };
}

function toSearch(params: DashboardParams, current: string): string {
  const q = new URLSearchParams(current); // keeps unrelated params such as ?lang=
  const set = (key: string, value: string | null, fallback: string | null) =>
    value === null || value === fallback ? q.delete(key) : q.set(key, value);

  set('preset', params.period.preset, DEFAULTS.period.preset);
  set('from', params.period.from ?? null, null);
  set('to', params.period.to ?? null, null);
  set('rankBy', params.rankBy, DEFAULTS.rankBy);
  set('productSort', params.productSort, DEFAULTS.productSort);
  set('status', params.saleStatus, null);
  set('page', String(params.salesPage), String(DEFAULTS.salesPage));
  const search = q.toString();
  return search ? `?${search}` : '';
}

const URL_CHANGED = 'dashboard:urlchange';

function subscribe(onChange: () => void) {
  window.addEventListener('popstate', onChange);
  window.addEventListener(URL_CHANGED, onChange);
  return () => {
    window.removeEventListener('popstate', onChange);
    window.removeEventListener(URL_CHANGED, onChange);
  };
}

const getSearch = () => window.location.search;

export function useDashboardParams() {
  const search = useSyncExternalStore(subscribe, getSearch);
  const params = useMemo(() => parseParams(search), [search]);

  const update = useCallback((patch: Partial<DashboardParams>) => {
    const current = parseParams(window.location.search);
    const next = { ...current, ...patch };
    // A new period or status filter changes the sales list, so it restarts at page 1.
    if (patch.period || 'saleStatus' in patch) next.salesPage = 1;

    const url = `${window.location.pathname}${toSearch(next, window.location.search)}`;
    // A new period is a new "view" worth a Back-button step; toggles and paging are not.
    if (patch.period) window.history.pushState(null, '', url);
    else window.history.replaceState(null, '', url);
    window.dispatchEvent(new Event(URL_CHANGED));
  }, []);

  return { params, update };
}
