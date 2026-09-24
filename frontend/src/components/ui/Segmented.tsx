import clsx from 'clsx';
import { motion } from 'framer-motion';
import { useId } from 'react';

interface SegmentedProps<T extends string> {
  options: readonly { value: T; label: string }[];
  value: T | null;
  onChange(value: T): void;
  label: string;
  size?: 'sm' | 'md';
}

/** A radio group styled as a pill switch; the highlight slides to the selected option. */
export function Segmented<T extends string>({ options, value, onChange, label, size = 'sm' }: SegmentedProps<T>) {
  const layoutId = useId();
  return (
    <div role="radiogroup" aria-label={label} className="inline-flex rounded-lg bg-slate-100 dark:bg-slate-800 p-0.5">
      {options.map((option) => {
        const selected = option.value === value;
        return (
          <button
            key={option.value}
            type="button"
            role="radio"
            aria-checked={selected}
            onClick={() => onChange(option.value)}
            className={clsx(
              'relative rounded-md font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-indigo-500',
              size === 'sm' ? 'px-2.5 py-1 text-xs' : 'px-3 py-1.5 text-sm',
              selected ? 'text-slate-900 dark:text-slate-100' : 'text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200',
            )}
          >
            {selected && (
              <motion.span
                layoutId={layoutId}
                className="absolute inset-0 rounded-md bg-white shadow-sm dark:bg-slate-700"
                transition={{ type: 'spring', bounce: 0.15, duration: 0.35 }}
              />
            )}
            <span className="relative whitespace-nowrap">{option.label}</span>
          </button>
        );
      })}
    </div>
  );
}
