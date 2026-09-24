import { QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createQueryClient } from '../api/queries';
import { Dashboard } from '../Dashboard';
import { I18nProvider } from '../i18n/I18nProvider';
import type { Lang } from '../i18n/format';
import { ThemeProvider, type Theme } from '../theme/ThemeProvider';

/** The real app wiring, with automatic retries off so an error state shows up at once. */
export function renderDashboard({ lang = 'ru', theme = 'light' }: { lang?: Lang; theme?: Theme } = {}) {
  const client = createQueryClient();
  client.setDefaultOptions({ queries: { ...client.getDefaultOptions().queries, retry: false } });

  const user = userEvent.setup();
  const view = render(
    <QueryClientProvider client={client}>
      <ThemeProvider theme={theme}>
        <I18nProvider lang={lang}>
          <Dashboard />
        </I18nProvider>
      </ThemeProvider>
    </QueryClientProvider>,
  );
  return { user, ...view };
}
