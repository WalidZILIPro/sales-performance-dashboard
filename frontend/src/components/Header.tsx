import { useIsFetching } from '@tanstack/react-query';
import { AnimatePresence, motion } from 'framer-motion';
import type { PeriodDto, PeriodParams } from '../api/types';
import { useI18n } from '../i18n/I18nProvider';
import type { Lang } from '../i18n/format';
import { useTheme } from '../theme/ThemeProvider';
import { PeriodPicker } from './PeriodPicker';
import { Segmented } from './ui/Segmented';

interface HeaderProps {
  period: PeriodParams;
  resolved: PeriodDto | undefined;
  onPeriodChange(period: PeriodParams): void;
}

const LANGS: readonly { value: Lang; label: string }[] = [
  { value: 'ru', label: 'RU' },
  { value: 'en', label: 'EN' },
];

export function Header({ period, resolved, onPeriodChange }: HeaderProps) {
  const { t, f, lang, setLang } = useI18n();
  const fetching = useIsFetching() > 0;

  return (
    <header className="sticky top-0 z-20 border-b border-slate-200/80 dark:border-slate-800 bg-white/85 dark:bg-slate-900/80 backdrop-blur">
      {/* Thin progress bar: the page stays readable while a new period loads (no skeleton flash). */}
      <AnimatePresence>
        {fetching && (
          <motion.div
            role="progressbar"
            aria-label={t.states.refreshing}
            className="absolute inset-x-0 top-0 h-0.5 origin-left bg-indigo-500"
            initial={{ scaleX: 0, opacity: 1 }}
            animate={{ scaleX: 0.85, transition: { duration: 2.5, ease: 'easeOut' } }}
            exit={{ scaleX: 1, opacity: 0, transition: { duration: 0.3 } }}
          />
        )}
      </AnimatePresence>

      <div className="mx-auto flex max-w-[1440px] items-center justify-between gap-6 px-8 py-3.5">
        <div className="flex items-center gap-3">
          <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-indigo-600 text-white shadow-sm">
            <svg aria-hidden viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round">
              <path d="M5 18v-5M10 18V7M15 18v-8M20 18V5" />
            </svg>
          </div>
          <div>
            <h1 className="text-base font-semibold leading-tight text-slate-900 dark:text-slate-100">{t.app.title}</h1>
            <p className="text-xs text-slate-500 dark:text-slate-400">{t.app.subtitle}</p>
          </div>
        </div>

        <div className="flex items-center gap-4">
          <div className="flex flex-col items-end gap-1">
            <PeriodPicker value={period} onChange={onPeriodChange} />
            <p className="h-4 text-[11px] text-slate-500 dark:text-slate-400" title={t.period.utcNote}>
              {resolved && (
                <>
                  <span className="font-medium text-slate-700 dark:text-slate-300">{f.range(resolved.from, resolved.to)}</span>
                  {' · '}
                  {t.period.days(resolved.days)}
                  {' · '}
                  {t.period.comparedTo(f.range(resolved.previousFrom, resolved.previousTo))}
                </>
              )}
            </p>
          </div>
          <div className="h-8 w-px bg-slate-200 dark:bg-slate-700" />
          <Segmented options={LANGS} value={lang} onChange={setLang} label={t.app.language} />
          <ThemeToggle />
        </div>
      </div>
    </header>
  );
}

function ThemeToggle() {
  const { t } = useI18n();
  const { theme, toggle } = useTheme();
  const dark = theme === 'dark';

  return (
    <button
      type="button"
      onClick={toggle}
      aria-label={t.app.darkTheme}
      aria-pressed={dark}
      title={dark ? t.app.switchToLight : t.app.switchToDark}
      className="relative flex h-8 w-8 items-center justify-center overflow-hidden rounded-lg bg-slate-100 text-slate-600 transition-colors hover:text-slate-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-indigo-500 dark:bg-slate-800 dark:text-slate-300 dark:hover:text-white"
    >
      <AnimatePresence mode="wait" initial={false}>
        <motion.svg
          key={theme}
          aria-hidden
          viewBox="0 0 24 24"
          className="h-4 w-4"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          initial={{ rotate: -90, opacity: 0 }}
          animate={{ rotate: 0, opacity: 1 }}
          exit={{ rotate: 90, opacity: 0 }}
          transition={{ duration: 0.2 }}
        >
          {dark ? (
            // Sun: shown in dark mode, since clicking it brings the light back.
            <>
              <circle cx="12" cy="12" r="4" />
              <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" />
            </>
          ) : (
            <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
          )}
        </motion.svg>
      </AnimatePresence>
    </button>
  );
}
