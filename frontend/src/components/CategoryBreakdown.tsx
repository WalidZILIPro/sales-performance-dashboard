import type { UseQueryResult } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import type { CategoryBreakdown as CategoryBreakdownData } from '../api/types';
import { useI18n } from '../i18n/I18nProvider';
import { Card } from './ui/Card';
import { QueryBlock } from './ui/QueryBlock';
import { Skeleton } from './ui/bits';

/**
 * One bar per category: its length is the revenue (relative to the biggest category), and the green
 * part inside it is the gross profit. Share and margin read at a glance without a second chart.
 */
export function CategoryBreakdown({ categories }: { categories: UseQueryResult<CategoryBreakdownData> }) {
  const { t, f } = useI18n();

  return (
    <Card aria-label={t.categories.title} title={t.categories.title} subtitle={t.categories.subtitle} dimmed={categories.isPlaceholderData} className="h-full">
      <QueryBlock
        query={categories}
        emptyText={t.categories.empty}
        isEmpty={(d) => d.categories.every((c) => c.revenue === 0)}
        skeleton={
          <div className="space-y-4">
            {Array.from({ length: 6 }, (_, i) => (
              <Skeleton key={i} className="h-8" />
            ))}
          </div>
        }
      >
        {(data) => {
          const max = Math.max(...data.categories.map((c) => c.revenue), 1);
          return (
            <ul className="space-y-2.5">
              {data.categories.map((c) => (
                <li key={c.categoryId}>
                  <div className="flex items-baseline justify-between gap-3 text-sm">
                    <span className="truncate font-medium text-slate-800 dark:text-slate-200">{c.name}</span>
                    <span className="shrink-0 font-semibold tabular-nums text-slate-900 dark:text-slate-100" title={f.money(c.revenue)}>
                      {f.moneyCompact(c.revenue)}
                    </span>
                  </div>
                  <div className="mt-1 h-1.5 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
                    <motion.div
                      className="flex h-full overflow-hidden rounded-full bg-indigo-500"
                      initial={false}
                      animate={{ width: `${(c.revenue / max) * 100}%` }}
                      transition={{ duration: 0.5, ease: 'easeOut' }}
                    >
                      <div className="ml-auto h-full bg-emerald-400" style={{ width: `${Math.max(0, (c.margin ?? 0) * 100)}%` }} />
                    </motion.div>
                  </div>
                  <p className="mt-0.5 text-[11px] tabular-nums text-slate-500 dark:text-slate-400">
                    {f.percent(c.revenueShare)} · {t.categories.margin} {f.percent(c.margin)}
                  </p>
                </li>
              ))}
            </ul>
          );
        }}
      </QueryBlock>
    </Card>
  );
}
