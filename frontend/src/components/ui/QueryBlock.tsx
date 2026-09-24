import type { UseQueryResult } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { ApiError } from '../../api/client';
import { useI18n } from '../../i18n/I18nProvider';

interface QueryBlockProps<T> {
  query: UseQueryResult<T>;
  skeleton: ReactNode;
  isEmpty?: (data: T) => boolean;
  emptyText: string;
  children: (data: T) => ReactNode;
}

/**
 * Every block goes through the same four states: loading (skeleton), error (message + retry),
 * empty (an explanation, never a silently blank area) and data. Each block has its own query, so
 * one failing endpoint never blanks the whole page.
 */
export function QueryBlock<T>({ query, skeleton, isEmpty, emptyText, children }: QueryBlockProps<T>) {
  // An error always wins: the data still on screen may be the previous period's placeholder, and
  // showing it without the error would present the wrong period's numbers as the answer.
  if (query.isError) {
    return <ErrorState error={query.error} onRetry={() => void query.refetch()} />;
  }
  if (query.data === undefined) {
    return <>{skeleton}</>;
  }
  if (isEmpty?.(query.data)) {
    return <EmptyState text={emptyText} />;
  }
  return <>{children(query.data)}</>;
}

export function EmptyState({ text }: { text: string }) {
  return (
    <div className="flex h-full min-h-[120px] flex-col items-center justify-center gap-2 text-center text-sm text-slate-500 dark:text-slate-400">
      <svg aria-hidden viewBox="0 0 24 24" className="h-6 w-6 text-slate-300 dark:text-slate-600" fill="none" stroke="currentColor" strokeWidth="1.8">
        <path d="M3 3v18h18M7 15l3-3 3 2 5-6" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
      {text}
    </div>
  );
}

function ErrorState({ error, onRetry }: { error: Error; onRetry(): void }) {
  const { t } = useI18n();
  const message = describeError(error, t.states);

  return (
    <div role="alert" className="flex h-full min-h-[120px] flex-col items-center justify-center gap-3 text-center">
      <div>
        <p className="text-sm font-medium text-slate-800 dark:text-slate-200">{t.states.errorTitle}</p>
        <p className="mt-1 max-w-sm text-xs text-slate-500 dark:text-slate-400">{message}</p>
      </div>
      <button
        type="button"
        onClick={onRetry}
        className="rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 px-3 py-1.5 text-xs font-medium text-slate-700 dark:text-slate-300 shadow-sm hover:bg-slate-50 dark:hover:bg-slate-800"
      >
        {t.states.retry}
      </button>
    </div>
  );
}

function describeError(error: Error, s: { errorNetwork: string; errorRateLimited: string; errorServer: string; errorValidation: string }) {
  if (!(error instanceof ApiError)) return s.errorServer;
  if (error.status === 0) return s.errorNetwork;
  if (error.status === 429) return s.errorRateLimited;
  if (error.status >= 500) return s.errorServer;
  const firstFieldError = Object.values(error.fieldErrors)[0]?.[0];
  return `${s.errorValidation}: ${firstFieldError ?? error.message}`;
}
