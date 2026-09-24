import { screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { emptySummary, managers } from './test/fixtures';
import { renderDashboard } from './test/render';
import { requests, server } from './test/server';

const lastRequest = (path: string) => [...requests].reverse().find((url) => url.pathname.endsWith(path));
const rankingRows = () =>
  within(screen.getByRole('region', { name: 'Рейтинг менеджеров' }))
    .getAllByRole('row')
    .slice(1) // header
    .map((row) => row.textContent ?? '');

beforeEach(() => {
  requests.length = 0;
});

describe('Dashboard', () => {
  it('shows skeletons first, then the data from the API', async () => {
    const { container } = renderDashboard();

    expect(container.querySelectorAll('.animate-pulse').length).toBeGreaterThan(0);
    expect(await screen.findAllByText('Алексей Смирнов')).not.toHaveLength(0);
    expect(screen.getByText('161,4 млн ₽')).toBeInTheDocument();
    // The revenue rule is visible next to the numbers, with correct Russian plurals.
    expect(screen.getByText(/Исключены: 17 отмен .* 16 возвратов/)).toBeInTheDocument();
  });

  it('changing the period refetches every block with the new preset and updates the URL', async () => {
    const { user } = renderDashboard();
    await screen.findAllByText('Алексей Смирнов');

    await user.click(screen.getByRole('radio', { name: '7 дней' }));

    await waitFor(() => {
      for (const path of ['/summary', '/trend', '/managers', '/categories', '/top', '/sales']) {
        expect(lastRequest(path)?.searchParams.get('preset')).toBe('Last7Days');
      }
    });
    expect(window.location.search).toContain('preset=Last7Days');
    expect(screen.getByRole('radio', { name: '7 дней' })).toHaveAttribute('aria-checked', 'true');
  });

  it('switching the ranking metric requests it from the server and re-orders the managers', async () => {
    const { user } = renderDashboard();
    await screen.findAllByText('Алексей Смирнов');
    expect(rankingRows()[0]).toContain('Алексей Смирнов');

    const ranking = screen.getByRole('region', { name: 'Рейтинг менеджеров' });
    await user.click(within(ranking).getByRole('radio', { name: 'Средний чек' }));

    await waitFor(() => expect(rankingRows()[0]).toContain('Мария Иванова'));
    expect(lastRequest('/managers')?.searchParams.get('rankBy')).toBe('AverageCheck');
    // A manager without sales is listed last and not ranked.
    expect(rankingRows().at(-1)).toContain('Нет продаж в этом периоде');
  });

  it('an API error is shown in the failing block only, and Retry recovers it', async () => {
    server.use(http.get('*/api/v1/dashboard/managers', () => HttpResponse.json({ title: 'boom' }, { status: 500 })));
    const { user } = renderDashboard();

    const ranking = await screen.findByRole('region', { name: 'Рейтинг менеджеров' });
    expect(await within(ranking).findByRole('alert')).toHaveTextContent('Не удалось загрузить блок');
    // Other blocks are unaffected.
    expect(screen.getByText('161,4 млн ₽')).toBeInTheDocument();

    server.use(http.get('*/api/v1/dashboard/managers', ({ request }) => HttpResponse.json(managers(new URL(request.url)))));
    await user.click(within(ranking).getByRole('button', { name: 'Повторить' }));

    await waitFor(() => expect(rankingRows()[0]).toContain('Алексей Смирнов'));
    expect(within(ranking).queryByRole('alert')).not.toBeInTheDocument();
  });

  it('an unreachable API is explained, not left as an endless spinner', async () => {
    server.use(http.get('*/api/v1/dashboard/summary', () => HttpResponse.error()));
    renderDashboard();

    const alerts = await screen.findAllByRole('alert');
    expect(alerts[0]).toHaveTextContent('Сервер недоступен');
  });

  it('a period without sales explains itself and offers a way out', async () => {
    server.use(http.get('*/api/v1/dashboard/summary', ({ request }) => HttpResponse.json(emptySummary(new URL(request.url)))));
    window.history.replaceState(null, '', '/?preset=Today');
    const { user } = renderDashboard();

    const banner = await screen.findByRole('status');
    expect(banner).toHaveTextContent('В этом периоде нет продаж');
    expect(screen.getByText('В этом периоде нет оплаченных продаж')).toBeInTheDocument();

    await user.click(within(banner).getByRole('button', { name: 'Показать последние 30 дней' }));
    await waitFor(() => expect(lastRequest('/summary')?.searchParams.get('preset')).toBe('Last30Days'));
  });

  it('switching language changes both the labels and the number format', async () => {
    const { user } = renderDashboard();
    await screen.findByText('161,4 млн ₽');

    await user.click(screen.getByRole('radio', { name: 'EN' }));

    expect(await screen.findByText('₽161.4M')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Manager ranking' })).toBeInTheDocument();
    expect(window.location.search).toContain('lang=en');
  });

  it('the theme toggle switches to dark mode and remembers the choice', async () => {
    const { user } = renderDashboard();
    await screen.findAllByText('Алексей Смирнов');
    const toggle = screen.getByRole('button', { name: 'Тёмная тема' });
    expect(toggle).toHaveAttribute('aria-pressed', 'false');

    await user.click(toggle);

    expect(toggle).toHaveAttribute('aria-pressed', 'true');
    expect(document.documentElement).toHaveClass('dark');
    expect(localStorage.getItem('sales-dashboard.theme')).toBe('dark');

    await user.click(toggle);
    expect(document.documentElement).not.toHaveClass('dark');
  });
});
