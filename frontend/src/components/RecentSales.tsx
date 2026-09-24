import type { UseQueryResult } from '@tanstack/react-query';
import clsx from 'clsx';
import type { ReactNode } from 'react';
import type { RecentSale, RecentSales as RecentSalesData, SaleStatus } from '../api/types';
import { SALE_STATUSES } from '../api/types';
import { useI18n } from '../i18n/I18nProvider';
import { Card } from './ui/Card';
import { QueryBlock } from './ui/QueryBlock';
import { Segmented } from './ui/Segmented';
import { Avatar, Skeleton, StatusBadge } from './ui/bits';

interface RecentSalesProps {
  sales: UseQueryResult<RecentSalesData>;
  status: SaleStatus | null;
  onStatusChange(status: SaleStatus | null): void;
  onPageChange(page: number): void;
}

const ALL = 'All';

export function RecentSales({ sales, status, onStatusChange, onPageChange }: RecentSalesProps) {
  const { t } = useI18n();
  const page = sales.data?.sales;

  return (
    <Card
      aria-label={t.sales.title}
      title={t.sales.title}
      subtitle={page ? t.sales.total(page.totalCount) : ' '}
      dimmed={sales.isPlaceholderData}
      actions={
        <Segmented
          label={t.sales.columns.status}
          value={status ?? ALL}
          onChange={(v) => onStatusChange(v === ALL ? null : (v as SaleStatus))}
          options={[{ value: ALL, label: t.sales.all }, ...SALE_STATUSES.map((s) => ({ value: s, label: t.status[s] }))]}
        />
      }
      bodyClassName="px-2 pb-2 pt-3"
    >
      <QueryBlock
        query={sales}
        emptyText={t.sales.empty}
        isEmpty={(d) => d.sales.items.length === 0}
        skeleton={
          <div className="space-y-2 px-3">
            {Array.from({ length: 10 }, (_, i) => (
              <Skeleton key={i} className="h-9" />
            ))}
          </div>
        }
      >
        {(data) => (
          <>
            <table className="w-full text-sm">
              <thead className="text-xs text-slate-500 dark:text-slate-400">
                <tr className="border-b border-slate-100 dark:border-slate-800">
                  <th scope="col" className="px-3 py-2 text-left font-medium">{t.sales.columns.date}</th>
                  <th scope="col" className="px-3 py-2 text-left font-medium">{t.sales.columns.manager}</th>
                  <th scope="col" className="px-3 py-2 text-left font-medium">{t.sales.columns.customer}</th>
                  <th scope="col" className="px-3 py-2 text-left font-medium">{t.sales.columns.items}</th>
                  <th scope="col" className="px-3 py-2 text-left font-medium">{t.sales.columns.status}</th>
                  <th scope="col" className="px-3 py-2 text-right font-medium">{t.sales.columns.amount}</th>
                  <th scope="col" className="px-3 py-2 text-right font-medium">{t.sales.columns.grossProfit}</th>
                </tr>
              </thead>
              <tbody>
                {data.sales.items.map((sale) => (
                  <SaleRow key={sale.id} sale={sale} />
                ))}
              </tbody>
            </table>

            <footer className="flex items-center justify-between px-3 pb-1 pt-3 text-xs text-slate-500 dark:text-slate-400">
              <span>{t.sales.page(data.sales.page, Math.max(1, data.sales.totalPages))}</span>
              <div className="flex gap-2">
                <PageButton disabled={data.sales.page <= 1} onClick={() => onPageChange(data.sales.page - 1)}>
                  ← {t.sales.previous}
                </PageButton>
                <PageButton disabled={data.sales.page >= data.sales.totalPages} onClick={() => onPageChange(data.sales.page + 1)}>
                  {t.sales.next} →
                </PageButton>
              </div>
            </footer>
          </>
        )}
      </QueryBlock>
    </Card>
  );
}

function SaleRow({ sale }: { sale: RecentSale }) {
  const { t, f } = useI18n();
  const counted = sale.status === 'Paid';
  const [first, ...rest] = sale.items;

  return (
    <tr className="border-b border-slate-50 dark:border-slate-800/60 last:border-0 hover:bg-slate-50/80 dark:hover:bg-slate-800/50">
      <td className="whitespace-nowrap px-3 py-2">
        <p className="tabular-nums text-slate-700 dark:text-slate-300">{f.dateTime(sale.soldAt)}</p>
        <p className="text-[11px] text-slate-400 dark:text-slate-500">{sale.number}</p>
      </td>
      <td className="px-3 py-2">
        <div className="flex items-center gap-2">
          <Avatar manager={sale.manager} size="sm" />
          <span className="truncate text-slate-800 dark:text-slate-200">{sale.manager.name}</span>
        </div>
      </td>
      <td className="max-w-[220px] px-3 py-2">
        <p className="truncate text-slate-800 dark:text-slate-200">{sale.customer.company}</p>
        <p className="truncate text-[11px] text-slate-400 dark:text-slate-500">{sale.customer.name}</p>
      </td>
      <td className="max-w-[260px] px-3 py-2" title={sale.items.map((i) => `${i.productName} ×${i.quantity}`).join('\n')}>
        {first && (
          <p className="truncate text-slate-700 dark:text-slate-300">
            {first.productName} <span className="text-slate-400 dark:text-slate-500">×{first.quantity}</span>
          </p>
        )}
        {rest.length > 0 && <p className="text-[11px] text-slate-400 dark:text-slate-500">{t.sales.moreItems(rest.length)}</p>}
      </td>
      <td className="px-3 py-2">
        <StatusBadge status={sale.status} />
      </td>
      <td className={clsx('px-3 py-2 text-right tabular-nums', counted ? 'text-slate-900 dark:text-slate-100' : 'text-slate-400 dark:text-slate-500')}>{f.money(sale.amount)}</td>
      {/* Cancelled and refunded sales are shown but struck through: they are not in any KPI. */}
      <td
        className={clsx('px-3 py-2 text-right tabular-nums', counted ? (sale.grossProfit < 0 ? 'text-rose-600 dark:text-rose-400' : 'text-emerald-700 dark:text-emerald-400') : 'text-slate-400 dark:text-slate-500 line-through')}
        title={counted ? undefined : t.sales.notCounted}
      >
        {f.money(sale.grossProfit)}
      </td>
    </tr>
  );
}

function PageButton({ disabled, onClick, children }: { disabled: boolean; onClick(): void; children: ReactNode }) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      className="rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 px-2.5 py-1 font-medium text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-40"
    >
      {children}
    </button>
  );
}
