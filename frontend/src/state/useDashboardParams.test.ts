import { describe, expect, it } from 'vitest';
import { parseParams } from './useDashboardParams';

describe('parseParams', () => {
  it('uses the defaults for an empty URL', () => {
    expect(parseParams('')).toEqual({
      period: { preset: 'Last30Days' },
      rankBy: 'GrossProfit',
      productSort: 'GrossProfit',
      saleStatus: null,
      salesPage: 1,
    });
  });

  it('reads a custom period with its dates', () => {
    expect(parseParams('?preset=Custom&from=2026-09-01&to=2026-09-10').period).toEqual({
      preset: 'Custom',
      from: '2026-09-01',
      to: '2026-09-10',
    });
  });

  it('ignores dates on a non-custom preset (the API would reject them)', () => {
    expect(parseParams('?preset=Last7Days&from=2026-09-01').period).toEqual({ preset: 'Last7Days' });
  });

  it('falls back to defaults for a hand-edited or stale URL instead of sending a bad request', () => {
    const p = parseParams('?preset=Yesterday&rankBy=Vibes&status=Lost&page=-3');
    expect(p.period.preset).toBe('Last30Days');
    expect(p.rankBy).toBe('GrossProfit');
    expect(p.saleStatus).toBeNull();
    expect(p.salesPage).toBe(1);
    expect(parseParams('?preset=Custom&from=2026-09-01').period).toEqual({ preset: 'Last30Days' });
  });
});
