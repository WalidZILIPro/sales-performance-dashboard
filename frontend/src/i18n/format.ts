export type Lang = 'ru' | 'en';

export const LOCALES: Record<Lang, string> = { ru: 'ru-RU', en: 'en-US' };

export interface Formatters {
  /** 12 400 000 ₽ / ₽12,400,000 */
  money(value: number | null): string;
  /** 12,4 млн ₽ / ₽12.4M: KPI cards and chart axes. */
  moneyCompact(value: number | null): string;
  /** 0.1568 → 15,7 % / 15.7% */
  percent(value: number | null, digits?: number): string;
  /** A change as a signed percent: +4.7% / −19.8%. */
  change(value: number | null): string;
  /** Margin difference in percentage points: +0.6 п.п. / +0.6 pp (a fraction in, 0.006 → 0.6). */
  points(value: number | null, unit: string): string;
  number(value: number | null): string;
  /** A yyyy-MM-dd UTC day: 24 сент. / Sep 24 */
  day(isoDate: string): string;
  /** A timestamp shown in UTC, like the periods: 24 сент., 11:40 */
  dateTime(isoDateTime: string): string;
  /** 26 авг. – 24 сент. 2026 г. / Aug 26 – Sep 24, 2026 */
  range(fromIso: string, toIso: string): string;
}

const DASH = '—';

/** A yyyy-MM-dd string is a UTC calendar day; parsing it at UTC midnight keeps it the same day everywhere. */
const utcDay = (iso: string) => new Date(`${iso}T00:00:00Z`);

export function createFormatters(lang: Lang): Formatters {
  const locale = LOCALES[lang];
  const money = new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: 'RUB',
    currencyDisplay: 'narrowSymbol',
    maximumFractionDigits: 0,
  });
  const moneyCompact = new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: 'RUB',
    currencyDisplay: 'narrowSymbol',
    notation: 'compact',
    maximumFractionDigits: 1,
  });
  const number = new Intl.NumberFormat(locale);
  const signed1 = new Intl.NumberFormat(locale, {
    style: 'percent',
    maximumFractionDigits: 1,
    minimumFractionDigits: 1,
    signDisplay: 'exceptZero',
  });
  const signedPoints = new Intl.NumberFormat(locale, {
    maximumFractionDigits: 1,
    minimumFractionDigits: 1,
    signDisplay: 'exceptZero',
  });
  const day = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short', timeZone: 'UTC' });
  const dateTime = new Intl.DateTimeFormat(locale, {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23',
    timeZone: 'UTC',
  });
  const range = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' });

  return {
    money: (v) => (v === null ? DASH : money.format(v)),
    moneyCompact: (v) => (v === null ? DASH : moneyCompact.format(v)),
    percent: (v, digits = 1) =>
      v === null
        ? DASH
        : new Intl.NumberFormat(locale, {
            style: 'percent',
            maximumFractionDigits: digits,
            minimumFractionDigits: digits,
          }).format(v),
    change: (v) => (v === null ? DASH : signed1.format(v)),
    points: (v, unit) => (v === null ? DASH : `${signedPoints.format(v * 100)} ${unit}`),
    number: (v) => (v === null ? DASH : number.format(v)),
    day: (iso) => day.format(utcDay(iso)),
    dateTime: (iso) => dateTime.format(new Date(iso)),
    range: (from, to) => (from === to ? range.format(utcDay(from)) : range.formatRange(utcDay(from), utcDay(to))),
  };
}
