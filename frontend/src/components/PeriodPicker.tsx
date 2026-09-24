import clsx from 'clsx';
import { AnimatePresence, motion } from 'framer-motion';
import { useEffect, useRef, useState } from 'react';
import type { PeriodParams, PeriodPreset } from '../api/types';
import { useI18n } from '../i18n/I18nProvider';
import { Segmented } from './ui/Segmented';

const QUICK_PRESETS: readonly Exclude<PeriodPreset, 'Custom'>[] = ['Today', 'Last7Days', 'Last30Days', 'ThisMonth', 'LastMonth'];

const todayUtc = () => new Date().toISOString().slice(0, 10);
const daysAgoUtc = (days: number) => new Date(Date.now() - days * 86_400_000).toISOString().slice(0, 10);

export function PeriodPicker({ value, onChange }: { value: PeriodParams; onChange(period: PeriodParams): void }) {
  const { t } = useI18n();
  const [open, setOpen] = useState(false);

  return (
    <div className="relative flex items-center gap-2">
      <Segmented
        size="md"
        label={t.period.label}
        options={QUICK_PRESETS.map((preset) => ({ value: preset, label: t.period[preset] }))}
        value={value.preset === 'Custom' ? null : value.preset}
        onChange={(preset) => {
          setOpen(false);
          onChange({ preset });
        }}
      />
      <button
        type="button"
        aria-expanded={open}
        aria-haspopup="dialog"
        onClick={() => setOpen((o) => !o)}
        className={clsx(
          'inline-flex items-center gap-1.5 rounded-lg border px-3 py-1.5 text-sm font-medium transition-colors',
          value.preset === 'Custom'
            ? 'border-indigo-200 dark:border-indigo-800 bg-indigo-50 dark:bg-indigo-500/15 text-indigo-700 dark:text-indigo-300'
            : 'border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800',
        )}
      >
        <svg aria-hidden viewBox="0 0 24 24" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
          <rect x="3" y="5" width="18" height="16" rx="2" />
          <path d="M3 10h18M8 3v4M16 3v4" />
        </svg>
        {t.period.Custom}
      </button>

      <AnimatePresence>
        {open && (
          <CustomRangePopover
            initial={value}
            onClose={() => setOpen(false)}
            onApply={(range) => {
              setOpen(false);
              onChange(range);
            }}
          />
        )}
      </AnimatePresence>
    </div>
  );
}

function CustomRangePopover({
  initial,
  onApply,
  onClose,
}: {
  initial: PeriodParams;
  onApply(period: PeriodParams): void;
  onClose(): void;
}) {
  const { t } = useI18n();
  const [from, setFrom] = useState(initial.from ?? daysAgoUtc(13));
  const [to, setTo] = useState(initial.to ?? todayUtc());
  const ref = useRef<HTMLDivElement>(null);

  // Validate before sending: the API would reject it with a 400 anyway, but the user deserves the
  // message next to the inputs, not in every block of the page.
  const invalid = !from || !to || from > to;

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    const onClick = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) onClose();
    };
    document.addEventListener('keydown', onKey);
    document.addEventListener('mousedown', onClick);
    return () => {
      document.removeEventListener('keydown', onKey);
      document.removeEventListener('mousedown', onClick);
    };
  }, [onClose]);

  return (
    <motion.div
      ref={ref}
      role="dialog"
      aria-label={t.period.Custom}
      initial={{ opacity: 0, y: -4, scale: 0.98 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      exit={{ opacity: 0, y: -4, scale: 0.98 }}
      transition={{ duration: 0.15 }}
      className="absolute right-0 top-full z-30 mt-2 w-72 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 p-4 shadow-lg"
    >
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (!invalid) onApply({ preset: 'Custom', from, to });
        }}
        className="space-y-3"
      >
        <div className="grid grid-cols-2 gap-3">
          <label className="text-xs font-medium text-slate-600 dark:text-slate-400">
            {t.period.from}
            <input
              type="date"
              value={from}
              max={todayUtc()}
              onChange={(e) => setFrom(e.target.value)}
              className="mt-1 w-full rounded-md border border-slate-200 dark:border-slate-700 px-2 py-1.5 text-sm text-slate-900 dark:text-slate-100"
            />
          </label>
          <label className="text-xs font-medium text-slate-600 dark:text-slate-400">
            {t.period.to}
            <input
              type="date"
              value={to}
              max={todayUtc()}
              onChange={(e) => setTo(e.target.value)}
              className="mt-1 w-full rounded-md border border-slate-200 dark:border-slate-700 px-2 py-1.5 text-sm text-slate-900 dark:text-slate-100"
            />
          </label>
        </div>
        {from && to && from > to && (
          <p role="alert" className="text-xs text-rose-600 dark:text-rose-400">
            {t.period.invalidRange}
          </p>
        )}
        <p className="text-[11px] text-slate-400 dark:text-slate-500">{t.period.utcNote}</p>
        <button
          type="submit"
          disabled={invalid}
          className="w-full rounded-lg bg-indigo-600 py-1.5 text-sm font-medium text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:bg-slate-300 dark:disabled:bg-slate-700"
        >
          {t.period.apply}
        </button>
      </form>
    </motion.div>
  );
}
