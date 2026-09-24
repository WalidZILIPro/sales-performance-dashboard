import type { UseQueryResult } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { useMemo } from 'react';
import { Area, AreaChart, ResponsiveContainer, YAxis } from 'recharts';
import type { DashboardSummary, MetricComparison, SalesTrend, TrendPoint } from '../api/types';
import { useI18n } from '../i18n/I18nProvider';
import { QueryBlock } from './ui/QueryBlock';
import { AnimatedNumber, Avatar, Delta, Skeleton } from './ui/bits';

interface KpiCardsProps {
  summary: UseQueryResult<DashboardSummary>;
  trend: UseQueryResult<SalesTrend>;
}

type KpiKey = keyof DashboardSummary['kpis'];

// Sparklines reuse the trend points the chart already loaded: no extra request. Ratios per bucket are
// a division of two server totals, not a recalculation of the dashboard.
const SPARK: Record<KpiKey, (p: TrendPoint) => number | null> = {
  revenue: (p) => p.revenue,
  grossProfit: (p) => p.grossProfit,
  margin: (p) => (p.revenue > 0 ? p.grossProfit / p.revenue : null),
  salesCount: (p) => p.salesCount,
  averageCheck: (p) => (p.salesCount > 0 ? p.revenue / p.salesCount : null),
};

export function KpiCards({ summary, trend }: KpiCardsProps) {
  const { t, f } = useI18n();
  const dimmed = summary.isPlaceholderData;

  // Memoised per language: AnimatedNumber restarts its count-up whenever `format` changes identity.
  const cards = useMemo<{ key: KpiKey; label: string; format: (v: number | null) => string; full: (v: number | null) => string }[]>(
    () => [
      { key: 'revenue', label: t.kpi.revenue, format: f.moneyCompact, full: f.money },
      { key: 'grossProfit', label: t.kpi.grossProfit, format: f.moneyCompact, full: f.money },
      { key: 'margin', label: t.kpi.margin, format: f.percent, full: f.percent },
      {
        key: 'salesCount',
        label: t.kpi.salesCount,
        format: (v) => f.number(v === null ? null : Math.round(v)),
        full: f.number,
      },
      { key: 'averageCheck', label: t.kpi.averageCheck, format: f.moneyCompact, full: f.money },
    ],
    [t, f],
  );

  return (
    <div className="space-y-2">
      <div className="grid grid-cols-6 gap-4">
        {cards.map((card, i) => (
          <motion.div
            key={card.key}
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: i * 0.04, duration: 0.35 }}
            className="rounded-2xl border border-slate-200/80 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 shadow-card"
          >
            <p className="text-xs font-medium text-slate-500 dark:text-slate-400">{card.label}</p>
            <QueryBlock
              query={summary}
              emptyText=""
              skeleton={
                <div className="mt-2 space-y-2">
                  <Skeleton className="h-7 w-28" />
                  <Skeleton className="h-4 w-16" />
                </div>
              }
            >
              {(data) => (
                <KpiValue
                  metric={data.kpis[card.key]}
                  format={card.format}
                  full={card.full}
                  points={card.key === 'margin'}
                  dimmed={dimmed}
                  spark={trend.data?.points.map((p) => SPARK[card.key](p))}
                />
              )}
            </QueryBlock>
          </motion.div>
        ))}

        <motion.div
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2, duration: 0.35 }}
          className="rounded-2xl border border-indigo-100 dark:border-indigo-900/60 bg-gradient-to-br from-indigo-50 dark:from-indigo-950/60 to-white dark:to-slate-900 p-4 shadow-card"
        >
          <p className="text-xs font-medium text-indigo-700 dark:text-indigo-300" title={t.kpi.bestManagerHint}>
            {t.kpi.bestManager}
          </p>
          <QueryBlock
            query={summary}
            emptyText=""
            skeleton={<Skeleton className="mt-2 h-10 w-full" />}
          >
            {(data) =>
              data.bestManager ? (
                <div className={dimmed ? 'opacity-50 transition-opacity' : 'transition-opacity'}>
                  <div className="mt-2 flex items-center gap-2.5">
                    <Avatar manager={data.bestManager.manager} size="lg" />
                    <div className="min-w-0">
                      <p className="truncate text-sm font-semibold text-slate-900 dark:text-slate-100">{data.bestManager.manager.name}</p>
                      <p className="truncate text-xs text-slate-500 dark:text-slate-400">{data.bestManager.manager.team}</p>
                    </div>
                  </div>
                  <div className="mt-2 flex items-center justify-between">
                    <span className="text-sm font-semibold tabular-nums text-slate-900 dark:text-slate-100" title={f.money(data.bestManager.grossProfit)}>
                      {f.moneyCompact(data.bestManager.grossProfit)}
                    </span>
                    <Delta value={data.bestManager.grossProfitChangePct} />
                  </div>
                </div>
              ) : (
                <p className="mt-3 text-xs text-slate-500 dark:text-slate-400">{t.kpi.noBestManager}</p>
              )
            }
          </QueryBlock>
        </motion.div>
      </div>

      {summary.data && <ExcludedNote summary={summary.data} />}
    </div>
  );
}

function KpiValue({
  metric,
  format,
  full,
  points,
  dimmed,
  spark,
}: {
  metric: MetricComparison;
  format: (v: number | null) => string;
  full: (v: number | null) => string;
  points: boolean;
  dimmed: boolean;
  spark: (number | null)[] | undefined;
}) {
  const { t } = useI18n();
  const previousText = metric.previous === null ? t.kpi.noComparison : t.kpi.previous(full(metric.previous));

  return (
    <div className={dimmed ? 'opacity-50 transition-opacity' : 'transition-opacity'}>
      <p className="mt-1.5 text-2xl font-semibold tracking-tight text-slate-900 dark:text-slate-100" title={full(metric.current)}>
        <AnimatedNumber value={metric.current} format={format} />
      </p>
      <div className="mt-1.5 flex items-end justify-between gap-2">
        <Delta value={points ? metric.change : metric.changePct} points={points} title={previousText} />
        {spark && spark.length > 1 && <Sparkline values={spark} />}
      </div>
    </div>
  );
}

function Sparkline({ values }: { values: (number | null)[] }) {
  const data = values.map((v, i) => ({ i, v }));
  return (
    <div className="h-8 w-20" aria-hidden>
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={data} margin={{ top: 2, right: 0, bottom: 0, left: 0 }}>
          <YAxis hide domain={['dataMin', 'dataMax']} />
          <Area
            type="monotone"
            dataKey="v"
            stroke="#6366f1"
            strokeWidth={1.5}
            fill="#6366f1"
            fillOpacity={0.08}
            connectNulls
            isAnimationActive={false}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}

/** Makes the revenue rule visible where the numbers are: cancelled and refunded sales are left out. */
function ExcludedNote({ summary }: { summary: DashboardSummary }) {
  const { t, f } = useI18n();
  const { cancelledCount, cancelledAmount, refundedCount, refundedAmount } = summary.statuses;
  return (
    <p className="px-1 text-xs text-slate-500 dark:text-slate-400">
      <span aria-hidden className="mr-1.5 inline-block h-1.5 w-1.5 -translate-y-px rounded-full bg-amber-400" />
      {t.kpi.excluded(
        t.kpi.cancelledPart(cancelledCount, f.moneyCompact(cancelledAmount)),
        t.kpi.refundedPart(refundedCount, f.moneyCompact(refundedAmount)),
      )}
    </p>
  );
}
