import { describe, expect, it } from 'vitest';
import { en } from './en';
import { createFormatters } from './format';
import { ru } from './ru';

// Intl uses non-breaking spaces in Russian numbers; compare on plain spaces.
const plain = (s: string) => s.replace(/[  ]/g, ' ');

describe('Russian plurals', () => {
  it.each([
    [1, '1 продажа'],
    [3, '3 продажи'],
    [5, '5 продаж'],
    [11, '11 продаж'],
    [21, '21 продажа'],
    [22, '22 продажи'],
  ])('%i → %s', (n, expected) => {
    expect(ru.sales.total(n)).toBe(expected);
  });

  it('English has two forms', () => {
    expect(en.sales.total(1)).toBe('1 sale');
    expect(en.sales.total(2)).toBe('2 sales');
  });
});

describe('formatters', () => {
  const ruF = createFormatters('ru');
  const enF = createFormatters('en');

  it('formats roubles for each language', () => {
    expect(plain(ruF.money(12_400_000))).toBe('12 400 000 ₽');
    expect(enF.money(12_400_000)).toBe('₽12,400,000');
    expect(plain(ruF.moneyCompact(12_400_000))).toBe('12,4 млн ₽');
    expect(enF.moneyCompact(12_400_000)).toBe('₽12.4M');
  });

  it('shows a margin change in percentage points, not percent', () => {
    expect(enF.points(0.006, 'pp')).toBe('+0.6 pp');
    expect(plain(ruF.points(-0.012, 'п.п.'))).toBe('-1,2 п.п.');
  });

  it('shows missing values as a dash instead of 0 or NaN', () => {
    expect(ruF.percent(null)).toBe('—');
    expect(ruF.change(null)).toBe('—');
  });

  it('reads yyyy-MM-dd as a UTC day, whatever the browser time zone', () => {
    expect(enF.day('2026-09-01')).toBe('Sep 1');
  });
});
