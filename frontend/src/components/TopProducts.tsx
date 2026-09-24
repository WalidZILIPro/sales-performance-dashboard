import type { UseQueryResult } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import type { ProductPerformance, ProductSortBy, TopProducts as TopProductsData } from '../api/types';
import { PRODUCT_SORTS } from '../api/types';
import { useI18n } from '../i18n/I18nProvider';
import { Card } from './ui/Card';
import { QueryBlock } from './ui/QueryBlock';
import { Segmented } from './ui/Segmented';
import { Skeleton } from './ui/bits';

interface TopProductsProps {
  products: UseQueryResult<TopProductsData>;
  sortBy: ProductSortBy;
  onSortChange(sort: ProductSortBy): void;
}

const VALUE: Record<ProductSortBy, (p: ProductPerformance) => number> = {
  GrossProfit: (p) => p.grossProfit,
  Revenue: (p) => p.revenue,
  Units: (p) => p.units,
};

export function TopProducts({ products, sortBy, onSortChange }: TopProductsProps) {
  const { t, f } = useI18n();
  const format = (p: ProductPerformance) =>
    sortBy === 'Units' ? t.products.units(p.units) : f.moneyCompact(VALUE[sortBy](p));

  return (
    <Card
      aria-label={t.products.title}
      title={t.products.title}
      dimmed={products.isPlaceholderData}
      className="h-full"
      actions={
        <Segmented
          label={t.products.title}
          value={sortBy}
          onChange={onSortChange}
          options={PRODUCT_SORTS.map((s) => ({ value: s, label: t.products.sortBy[s] }))}
        />
      }
    >
      <QueryBlock
        query={products}
        emptyText={t.products.empty}
        isEmpty={(d) => d.products.length === 0}
        skeleton={
          <div className="space-y-3">
            {Array.from({ length: 8 }, (_, i) => (
              <Skeleton key={i} className="h-9" />
            ))}
          </div>
        }
      >
        {(data) => {
          const max = Math.max(...data.products.map(VALUE[sortBy]), 1);
          return (
            <ol className="space-y-3">
              {data.products.map((p, i) => (
                <motion.li layout="position" key={p.productId} className="flex items-center gap-3">
                  <span className="w-4 text-right text-xs font-semibold tabular-nums text-slate-400 dark:text-slate-500">{i + 1}</span>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-baseline justify-between gap-2">
                      <p className="truncate text-sm font-medium text-slate-800 dark:text-slate-200" title={p.name}>
                        {p.name}
                      </p>
                      <span className="shrink-0 text-sm font-semibold tabular-nums text-slate-900 dark:text-slate-100">{format(p)}</span>
                    </div>
                    <p className="text-[11px] text-slate-400 dark:text-slate-500">
                      {p.category} · {t.categories.margin} {f.percent(p.margin)}
                    </p>
                    <div className="mt-1">
                      <div className="h-1 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
                        <motion.div
                          className="h-full rounded-full bg-indigo-400"
                          initial={false}
                          animate={{ width: `${(Math.max(0, VALUE[sortBy](p)) / max) * 100}%` }}
                          transition={{ duration: 0.45 }}
                        />
                      </div>
                    </div>
                  </div>
                </motion.li>
              ))}
            </ol>
          );
        }}
      </QueryBlock>
    </Card>
  );
}
