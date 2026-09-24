import clsx from 'clsx';
import type { ReactNode } from 'react';

interface CardProps {
  title?: ReactNode;
  subtitle?: ReactNode;
  actions?: ReactNode;
  /** Old data shown while the new period loads: dim it so it does not read as the answer. */
  dimmed?: boolean;
  className?: string;
  bodyClassName?: string;
  children: ReactNode;
  'aria-label'?: string;
}

export function Card({ title, subtitle, actions, dimmed, className, bodyClassName, children, ...rest }: CardProps) {
  return (
    <section
      aria-label={rest['aria-label']}
      aria-busy={dimmed || undefined}
      className={clsx('flex flex-col rounded-2xl border border-slate-200/80 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-card', className)}
    >
      {(title || actions) && (
        <header className="flex items-start justify-between gap-4 px-5 pt-4">
          <div className="min-w-0">
            {title && <h2 className="text-[15px] font-semibold text-slate-900 dark:text-slate-100">{title}</h2>}
            {subtitle && <p className="mt-0.5 text-xs text-slate-500 dark:text-slate-400">{subtitle}</p>}
          </div>
          {actions && <div className="shrink-0">{actions}</div>}
        </header>
      )}
      <div className={clsx('flex-1 transition-opacity duration-300', dimmed && 'opacity-50', bodyClassName ?? 'p-5')}>
        {children}
      </div>
    </section>
  );
}
