import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { en, type Messages } from './en';
import { ru } from './ru';
import { createFormatters, type Formatters, type Lang } from './format';

const MESSAGES: Record<Lang, Messages> = { ru, en };
const STORAGE_KEY = 'sales-dashboard.lang';

interface I18nContextValue {
  lang: Lang;
  setLang(lang: Lang): void;
  /** Typed messages: `t.kpi.revenue`, `t.sales.total(3)`. No string keys to mistype. */
  t: Messages;
  /** Number and date formatting in the current language. */
  f: Formatters;
}

const I18nContext = createContext<I18nContextValue | null>(null);

const isLang = (value: unknown): value is Lang => value === 'ru' || value === 'en';

/** ?lang= in the URL wins (shareable links), then the last choice, then Russian: the client's language. */
function initialLang(): Lang {
  const fromUrl = new URLSearchParams(window.location.search).get('lang');
  if (isLang(fromUrl)) return fromUrl;
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (isLang(stored)) return stored;
  } catch {
    // storage blocked (private mode): fall through to the default
  }
  return 'ru';
}

export function I18nProvider({ children, lang: forcedLang }: { children: ReactNode; lang?: Lang }) {
  const [lang, setLangState] = useState<Lang>(() => forcedLang ?? initialLang());

  const setLang = useCallback((next: Lang) => {
    setLangState(next);
    try {
      localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // not critical
    }
    const url = new URL(window.location.href);
    url.searchParams.set('lang', next);
    window.history.replaceState(window.history.state, '', url);
  }, []);

  useEffect(() => {
    document.documentElement.lang = lang;
  }, [lang]);

  const value = useMemo(() => ({ lang, setLang, t: MESSAGES[lang], f: createFormatters(lang) }), [lang, setLang]);
  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n(): I18nContextValue {
  const context = useContext(I18nContext);
  if (!context) throw new Error('useI18n must be used inside <I18nProvider>');
  return context;
}
