import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';

export type Theme = 'light' | 'dark';

/** Same key as the inline script in index.html, which applies the theme before the first paint. */
const STORAGE_KEY = 'sales-dashboard.theme';

interface ThemeContextValue {
  theme: Theme;
  setTheme(theme: Theme): void;
  toggle(): void;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

/** The last explicit choice wins; otherwise follow the operating system. */
function initialTheme(): Theme {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'light' || stored === 'dark') return stored;
  } catch {
    // storage blocked (private mode): fall through to the system preference
  }
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

export function ThemeProvider({ children, theme: forcedTheme }: { children: ReactNode; theme?: Theme }) {
  const [theme, setThemeState] = useState<Theme>(() => forcedTheme ?? initialTheme());

  const setTheme = useCallback((next: Theme) => {
    setThemeState(next);
    try {
      localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // not critical
    }
  }, []);

  // Tailwind's `dark:` variants key off this class (darkMode: 'class').
  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark');
  }, [theme]);

  const value = useMemo(
    () => ({ theme, setTheme, toggle: () => setTheme(theme === 'dark' ? 'light' : 'dark') }),
    [theme, setTheme],
  );
  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme(): ThemeContextValue {
  const context = useContext(ThemeContext);
  if (!context) throw new Error('useTheme must be used inside <ThemeProvider>');
  return context;
}
