import { AnimatePresence, motion } from 'framer-motion';
import { useCategories, useManagerRanking, useRecentSales, useSummary, useTopProducts, useTrend } from './api/queries';
import { CategoryBreakdown } from './components/CategoryBreakdown';
import { Header } from './components/Header';
import { KpiCards } from './components/KpiCards';
import { ManagerRanking } from './components/ManagerRanking';
import { RecentSales } from './components/RecentSales';
import { TopProducts } from './components/TopProducts';
import { TrendChart } from './components/TrendChart';
import { useI18n } from './i18n/I18nProvider';
import { useDashboardParams } from './state/useDashboardParams';

export function Dashboard() {
  const { params, update } = useDashboardParams();
  const { period } = params;

  // One query per block: they load in parallel, fail independently, and each shows its own state.
  const summary = useSummary(period);
  const trend = useTrend(period);
  const ranking = useManagerRanking(period, params.rankBy);
  const categories = useCategories(period);
  const products = useTopProducts(period, params.productSort);
  const sales = useRecentSales(period, params.salesPage, params.saleStatus);

  const statuses = summary.data?.statuses;
  const periodIsEmpty =
    !summary.isPlaceholderData && statuses !== undefined && statuses.paidCount + statuses.cancelledCount + statuses.refundedCount === 0;

  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-950">
      <Header period={period} resolved={summary.data?.period} onPeriodChange={(p) => update({ period: p })} />

      <main className="mx-auto max-w-[1440px] space-y-5 px-8 py-6">
        <AnimatePresence>
          {periodIsEmpty && period.preset !== 'Last30Days' && (
            <EmptyPeriodBanner onShowLast30={() => update({ period: { preset: 'Last30Days' } })} />
          )}
        </AnimatePresence>

        <KpiCards summary={summary} trend={trend} />

        <div className="grid grid-cols-12 gap-5">
          <div className="col-span-8">
            <TrendChart trend={trend} />
          </div>
          <div className="col-span-4">
            <CategoryBreakdown categories={categories} />
          </div>

          <div className="col-span-8">
            <ManagerRanking ranking={ranking} rankBy={params.rankBy} onRankByChange={(rankBy) => update({ rankBy })} />
          </div>
          <div className="col-span-4">
            <TopProducts products={products} sortBy={params.productSort} onSortChange={(productSort) => update({ productSort })} />
          </div>

          <div className="col-span-12">
            <RecentSales
              sales={sales}
              status={params.saleStatus}
              onStatusChange={(saleStatus) => update({ saleStatus })}
              onPageChange={(salesPage) => update({ salesPage })}
            />
          </div>
        </div>
      </main>
    </div>
  );
}

function EmptyPeriodBanner({ onShowLast30 }: { onShowLast30(): void }) {
  const { t } = useI18n();
  return (
    <motion.div
      role="status"
      initial={{ opacity: 0, height: 0 }}
      animate={{ opacity: 1, height: 'auto' }}
      exit={{ opacity: 0, height: 0 }}
      className="overflow-hidden"
    >
      <div className="flex items-center justify-between gap-4 rounded-2xl border border-amber-200 dark:border-amber-900/60 bg-amber-50 dark:bg-amber-500/10 px-5 py-3.5">
        <div>
          <p className="text-sm font-semibold text-amber-900 dark:text-amber-200">{t.states.emptyPeriodTitle}</p>
          <p className="text-xs text-amber-800/80 dark:text-amber-300/80">{t.states.emptyPeriodBody}</p>
        </div>
        <button
          type="button"
          onClick={onShowLast30}
          className="shrink-0 rounded-lg bg-amber-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-amber-500"
        >
          {t.states.showLast30}
        </button>
      </div>
    </motion.div>
  );
}
