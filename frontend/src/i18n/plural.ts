export interface PluralForms {
  one: string;
  /** Russian 2–4 (and 22–24, ...): "продажи". */
  few?: string;
  /** Russian 5–20, 0 (and 25–30, ...): "продаж". */
  many?: string;
  other: string;
}

const rulesCache = new Map<string, Intl.PluralRules>();

/**
 * Russian has three plural forms (1 продажа, 3 продажи, 5 продаж, 21 продажа), so `n === 1 ? a : b`
 * is wrong. Intl.PluralRules knows each language's rules.
 */
export function plural(locale: string, n: number, forms: PluralForms): string {
  let rules = rulesCache.get(locale);
  if (!rules) {
    rules = new Intl.PluralRules(locale);
    rulesCache.set(locale, rules);
  }

  const category = rules.select(n);
  const form = category === 'one' ? forms.one : category === 'few' ? forms.few : category === 'many' ? forms.many : undefined;
  return form ?? forms.other;
}
