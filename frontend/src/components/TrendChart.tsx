import type { UseQueryResult } from '@tanstack/react-query';
import { useState } from 'react';
import { Area, AreaChart, Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import type { SalesTrend } from '../api/types';
import { useI18n } from '../i18n/I18nProvider';
import { useTheme, type Theme } from '../theme/ThemeProvider';
import { Card } from './ui/Card';
import { QueryBlock } from './ui/QueryBlock';
import { Segmented } from './ui/Segmented';
import { Skeleton } from './ui/bits';

type View = 'money' | 'count';

// SVG attributes cannot use Tailwind's dark: variants, so the chart takes its colours from the theme.
// The series are one step lighter in dark mode to keep their contrast against the dark card.
type ChartColors = Record<'revenue' | 'profit' | 'grid' | 'tick' | 'cursor' | 'tooltipBg' | 'tooltipBorder' | 'tooltipText', string>;

const PALETTE: Record<Theme, ChartColors> = {
  light: { revenue: '#4f46e5', profit: '#10b981', grid: '#f1f5f9', tick: '#64748b', cursor: '#f8fafc', tooltipBg: '#ffffff', tooltipBorder: '#e2e8f0', tooltipText: '#0f172a' },
  dark: { revenue: '#818cf8', profit: '#34d399', grid: '#1e293b', tick: '#94a3b8', cursor: '#1e293b', tooltipBg: '#0f172a', tooltipBorder: '#334155', tooltipText: '#f1f5f9' },
};

export function TrendChart({ trend }: { trend: UseQueryResult<SalesTrend> }) {
  const { t, f } = useI18n();
  const [view, setView] = useState<View>('money');
  const c = PALETTE[useTheme().theme];
  const tick = { fontSize: 11, fill: c.tick };

  return (
    <Card
      aria-label={t.trend.title}
      title={t.trend.title}
      subtitle={trend.data ? t.trend.granularity[trend.data.granularity] : ' '}
      dimmed={trend.isPlaceholderData}
      actions={
        <Segmented<View>
          label={t.trend.title}
          value={view}
          onChange={setView}
          options={[
            { value: 'money', label: t.trend.revenueAndProfit },
            { value: 'count', label: t.trend.salesCount },
          ]}
        />
      }
      className="h-full"
      bodyClassName="flex flex-col px-3 pb-3 pt-2"
    >
      <QueryBlock
        query={trend}
        emptyText={t.trend.empty}
        isEmpty={(d) => d.points.every((p) => p.salesCount === 0)}
        skeleton={<Skeleton className="m-2 h-[260px]" />}
      >
        {(data) => {
          const tooltip = {
            labelFormatter: (label: string) => f.day(label),
            contentStyle: { borderRadius: 10, borderColor: c.tooltipBorder, backgroundColor: c.tooltipBg, color: c.tooltipText, fontSize: 12 },
            labelStyle: { color: c.tooltipText },
          };
          return (
            <div className="min-h-[276px] flex-1">
              <ResponsiveContainer width="100%" height="100%">
                {view === 'money' ? (
                  <AreaChart data={data.points} margin={{ top: 8, right: 12, left: 4, bottom: 0 }}>
                    <defs>
                      <linearGradient id="fill-revenue" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor={c.revenue} stopOpacity={0.22} />
                        <stop offset="100%" stopColor={c.revenue} stopOpacity={0} />
                      </linearGradient>
                      <linearGradient id="fill-profit" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor={c.profit} stopOpacity={0.25} />
                        <stop offset="100%" stopColor={c.profit} stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid vertical={false} stroke={c.grid} />
                    <XAxis dataKey="bucketStart" tickFormatter={f.day} tick={tick} tickLine={false} axisLine={false} minTickGap={24} />
                    <YAxis tickFormatter={(v: number) => f.moneyCompact(v)} tick={tick} tickLine={false} axisLine={false} width={72} />
                    <Tooltip {...tooltip} formatter={(v: number, name: string) => [f.money(v), name]} />
                    <Area type="monotone" dataKey="revenue" name={t.kpi.revenue} stroke={c.revenue} strokeWidth={2} fill="url(#fill-revenue)" animationDuration={500} />
                    <Area type="monotone" dataKey="grossProfit" name={t.kpi.grossProfit} stroke={c.profit} strokeWidth={2} fill="url(#fill-profit)" animationDuration={500} />
                  </AreaChart>
                ) : (
                  <BarChart data={data.points} margin={{ top: 8, right: 12, left: 4, bottom: 0 }}>
                    <CartesianGrid vertical={false} stroke={c.grid} />
                    <XAxis dataKey="bucketStart" tickFormatter={f.day} tick={tick} tickLine={false} axisLine={false} minTickGap={24} />
                    <YAxis allowDecimals={false} tick={tick} tickLine={false} axisLine={false} width={40} />
                    <Tooltip {...tooltip} cursor={{ fill: c.cursor }} formatter={(v: number, name: string) => [f.number(v), name]} />
                    <Bar dataKey="salesCount" name={t.kpi.salesCount} fill={c.revenue} radius={[4, 4, 0, 0]} maxBarSize={28} animationDuration={500} />
                  </BarChart>
                )}
              </ResponsiveContainer>
            </div>
          );
        }}
      </QueryBlock>
      {view === 'money' && trend.data && (
        <div className="flex gap-4 px-3 pt-1 text-xs text-slate-500 dark:text-slate-400">
          <LegendDot color={c.revenue} label={t.kpi.revenue} />
          <LegendDot color={c.profit} label={t.kpi.grossProfit} />
        </div>
      )}
    </Card>
  );
}

function LegendDot({ color, label }: { color: string; label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5">
      <span className="h-2 w-2 rounded-full" style={{ backgroundColor: color }} />
      {label}
    </span>
  );
}
