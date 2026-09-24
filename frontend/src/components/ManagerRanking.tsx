import type { UseQueryResult } from '@tanstack/react-query';
import clsx from 'clsx';
import { motion } from 'framer-motion';
import type { ManagerRanking as ManagerRankingData, ManagerRankingRow, RankingMetric } from '../api/types';
import { RANKING_METRICS } from '../api/types';
import { useI18n } from '../i18n/I18nProvider';
import { Card } from './ui/Card';
import { QueryBlock } from './ui/QueryBlock';
import { Segmented } from './ui/Segmented';
import { Avatar, Delta, Skeleton } from './ui/bits';

interface ManagerRankingProps {
  ranking: UseQueryResult<ManagerRankingData>;
  rankBy: RankingMetric;
  onRankByChange(metric: RankingMetric): void;
}

type Column = 'salesCount' | 'revenue' | 'grossProfit' | 'averageCheck' | 'margin';

/** The column that holds the value being ranked on is highlighted. */
const METRIC_COLUMN: Record<RankingMetric, Column> = {
  GrossProfit: 'grossProfit',
  AverageCheck: 'averageCheck',
  Revenue: 'revenue',
  Margin: 'margin',
};

const MEDALS = ['bg-amber-100 dark:bg-amber-500/15 text-amber-800 dark:text-amber-300', 'bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300', 'bg-orange-100 dark:bg-orange-500/15 text-orange-800 dark:text-orange-300'];

export function ManagerRanking({ ranking, rankBy, onRankByChange }: ManagerRankingProps) {
  const { t, f } = useI18n();
  const highlighted = METRIC_COLUMN[rankBy];

  const columns: { key: Column; label: string; render: (row: ManagerRankingRow) => string }[] = [
    { key: 'salesCount', label: t.ranking.columns.sales, render: (r) => f.number(r.salesCount) },
    { key: 'revenue', label: t.ranking.columns.revenue, render: (r) => f.moneyCompact(r.revenue) },
    { key: 'grossProfit', label: t.ranking.columns.grossProfit, render: (r) => f.moneyCompact(r.grossProfit) },
    { key: 'averageCheck', label: t.ranking.columns.averageCheck, render: (r) => f.moneyCompact(r.averageCheck) },
    { key: 'margin', label: t.ranking.columns.margin, render: (r) => f.percent(r.margin) },
  ];

  return (
    <Card
      aria-label={t.ranking.title}
      title={t.ranking.title}
      dimmed={ranking.isPlaceholderData}
      actions={
        <Segmented
          label={t.ranking.rankBy}
          value={rankBy}
          onChange={onRankByChange}
          options={RANKING_METRICS.map((m) => ({ value: m, label: t.ranking.metrics[m] }))}
        />
      }
      className="h-full"
      bodyClassName="px-2 pb-2 pt-3"
    >
      <QueryBlock
        query={ranking}
        emptyText={t.ranking.empty}
        isEmpty={(d) => d.managers.length === 0}
        skeleton={
          <div className="space-y-2 px-3">
            {Array.from({ length: 8 }, (_, i) => (
              <Skeleton key={i} className="h-9" />
            ))}
          </div>
        }
      >
        {(data) => (
          <div className="max-h-[488px] overflow-auto">
            <table className="w-full text-sm">
              <thead className="sticky top-0 z-10 bg-white dark:bg-slate-900 text-xs text-slate-500 dark:text-slate-400">
                <tr className="border-b border-slate-100 dark:border-slate-800">
                  <th scope="col" className="w-12 px-2 py-2 text-left font-medium">{t.ranking.columns.rank}</th>
                  <th scope="col" className="px-3 py-2 text-left font-medium">{t.ranking.columns.manager}</th>
                  {columns.map((c) => (
                    <th
                      key={c.key}
                      scope="col"
                      className={clsx('whitespace-nowrap px-2 py-2 text-right font-medium', c.key === highlighted && 'text-indigo-700 dark:text-indigo-300')}
                    >
                      {c.label}
                    </th>
                  ))}
                  <th scope="col" className="whitespace-nowrap px-2 py-2 text-right font-medium">{t.ranking.columns.change}</th>
                </tr>
              </thead>
              <tbody>
                {data.managers.map((row) => {
                  const tied = row.rank !== null && data.managers.some((o) => o !== row && o.rank === row.rank);
                  return (
                    // `layout` animates each row to its new position when the metric or period changes.
                    <motion.tr
                      layout="position"
                      transition={{ type: 'spring', stiffness: 500, damping: 40 }}
                      key={row.manager.id}
                      className={clsx('border-b border-slate-50 dark:border-slate-800/60 last:border-0 hover:bg-slate-50/80 dark:hover:bg-slate-800/50', !row.hasSales && 'text-slate-400 dark:text-slate-500')}
                    >
                      <td className="px-2 py-2">
                        <RankCell row={row} tied={tied} />
                      </td>
                      <td className="max-w-[210px] px-2 py-2">
                        <div className="flex items-center gap-2.5">
                          <Avatar manager={row.manager} size="sm" />
                          <div className="min-w-0">
                            <p className={clsx('truncate font-medium', row.hasSales ? 'text-slate-900 dark:text-slate-100' : 'text-slate-400 dark:text-slate-500')}>{row.manager.name}</p>
                            <p className="truncate text-[11px] text-slate-400 dark:text-slate-500">
                              {row.hasSales ? `${row.manager.team} · ${row.manager.title}` : t.ranking.noSales}
                            </p>
                          </div>
                        </div>
                      </td>
                      {columns.map((c) => (
                        <td
                          key={c.key}
                          className={clsx(
                            'whitespace-nowrap px-2 py-2 text-right tabular-nums',
                            c.key === highlighted && row.hasSales && 'font-semibold text-slate-900 dark:text-slate-100',
                          )}
                        >
                          {row.hasSales ? c.render(row) : '—'}
                        </td>
                      ))}
                      <td className="px-2 py-2 text-right">{row.hasSales && <Delta value={row.metricChangePct} />}</td>
                    </motion.tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </QueryBlock>
    </Card>
  );
}

function RankCell({ row, tied }: { row: ManagerRankingRow; tied: boolean }) {
  const { t } = useI18n();
  if (row.rank === null) return <span className="pl-2 text-slate-300 dark:text-slate-600">—</span>;

  return (
    <div className="flex items-center gap-1">
      <span
        title={tied ? t.ranking.tie : undefined}
        className={clsx(
          'inline-flex h-6 min-w-6 items-center justify-center rounded-md px-1 text-xs font-semibold tabular-nums',
          MEDALS[row.rank - 1] ?? 'text-slate-600 dark:text-slate-400',
        )}
      >
        {tied ? `=${row.rank}` : row.rank}
      </span>
      {row.rankChange !== null && row.rankChange !== 0 && (
        <span
          title={row.rankChange > 0 ? t.ranking.movedUp(row.rankChange) : t.ranking.movedDown(-row.rankChange)}
          className={clsx('text-[10px] font-medium tabular-nums', row.rankChange > 0 ? 'text-emerald-600 dark:text-emerald-400' : 'text-rose-500 dark:text-rose-400')}
        >
          {row.rankChange > 0 ? '↑' : '↓'}
          {Math.abs(row.rankChange)}
        </span>
      )}
    </div>
  );
}
