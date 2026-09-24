import clsx from 'clsx';
import { animate, useReducedMotion } from 'framer-motion';
import { useEffect, useRef } from 'react';
import type { ManagerRef, SaleStatus } from '../../api/types';
import { useI18n } from '../../i18n/I18nProvider';

export function Skeleton({ className }: { className?: string }) {
  return <div aria-hidden className={clsx('animate-pulse rounded-md bg-slate-200/70 dark:bg-slate-800', className)} />;
}

export function Avatar({ manager, size = 'md' }: { manager: ManagerRef; size?: 'sm' | 'md' | 'lg' }) {
  return (
    <span
      aria-hidden
      style={{ backgroundColor: manager.avatarColor }}
      className={clsx(
        'inline-flex shrink-0 items-center justify-center rounded-full font-semibold text-white',
        size === 'sm' && 'h-7 w-7 text-[11px]',
        size === 'md' && 'h-8 w-8 text-xs',
        size === 'lg' && 'h-10 w-10 text-sm',
      )}
    >
      {manager.initials}
    </span>
  );
}

/**
 * A change against the previous period: green up, red down.
 * `points` shows a margin difference in percentage points rather than a relative percent.
 */
export function Delta({ value, points, title }: { value: number | null; points?: boolean; title?: string }) {
  const { t, f } = useI18n();
  if (value === null) {
    return (
      <span title={title} className="text-xs text-slate-400 dark:text-slate-500">
        —
      </span>
    );
  }

  const direction = value > 0 ? 'up' : value < 0 ? 'down' : 'flat';
  return (
    <span
      title={title}
      className={clsx(
        'inline-flex items-center gap-0.5 rounded-md px-1.5 py-0.5 text-xs font-medium tabular-nums',
        direction === 'up' && 'bg-emerald-50 dark:bg-emerald-500/10 text-emerald-700 dark:text-emerald-400',
        direction === 'down' && 'bg-rose-50 dark:bg-rose-500/10 text-rose-700 dark:text-rose-400',
        direction === 'flat' && 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400',
      )}
    >
      <span aria-hidden>{direction === 'up' ? '▲' : direction === 'down' ? '▼' : '•'}</span>
      {points ? f.points(value, t.kpi.pp) : f.change(value)}
    </span>
  );
}

const STATUS_STYLES: Record<SaleStatus, string> = {
  Paid: 'bg-emerald-50 dark:bg-emerald-500/10 text-emerald-700 dark:text-emerald-400 ring-emerald-600/20',
  Cancelled: 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 ring-slate-500/20',
  Refunded: 'bg-amber-50 dark:bg-amber-500/10 text-amber-700 dark:text-amber-400 ring-amber-600/20',
};

export function StatusBadge({ status }: { status: SaleStatus }) {
  const { t } = useI18n();
  return (
    <span className={clsx('inline-flex rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset', STATUS_STYLES[status])}>
      {t.status[status]}
    </span>
  );
}

/** Counts up to the new value when it changes; instant when the user prefers reduced motion. */
export function AnimatedNumber({ value, format }: { value: number | null; format: (value: number | null) => string }) {
  const ref = useRef<HTMLSpanElement>(null);
  const previous = useRef(0);
  const reduceMotion = useReducedMotion();

  useEffect(() => {
    const node = ref.current;
    if (!node || value === null || reduceMotion) {
      if (value !== null) previous.current = value;
      return;
    }
    const controls = animate(previous.current, value, {
      duration: 0.7,
      ease: [0.16, 1, 0.3, 1],
      onUpdate: (latest) => {
        node.textContent = format(latest);
      },
    });
    previous.current = value;
    return () => controls.stop();
  }, [value, format, reduceMotion]);

  return (
    <span ref={ref} className="tabular-nums">
      {format(value)}
    </span>
  );
}
