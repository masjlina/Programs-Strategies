import { useRef, type CSSProperties } from "react";
import type { SystemStats } from "../../lib/systemStats";
import { useCountUp } from "../../hooks/useCountUp";
import { useInView } from "../../hooks/useInView";
import { StatTile } from "./StatTile";

interface SystemDashboardProps {
  stats: SystemStats;
}

const CHART_HEIGHT_PX = 120;

function formatCount(value: number): string {
  return value.toLocaleString("uk-UA");
}

function formatDayLabel(date: string): string {
  const parsed = new Date(`${date}T00:00:00`);
  return parsed.toLocaleDateString("uk-UA", { day: "numeric", month: "short" });
}

export function SystemDashboard({ stats }: SystemDashboardProps) {
  const panelRef = useRef<HTMLElement>(null);
  const chartRef = useRef<HTMLElement>(null);
  const panelInView = useInView(panelRef);
  const chartInView = useInView(chartRef);

  const maxDailyCount = Math.max(
    1,
    ...stats.strategiesLastMonthByDay.map((item) => item.count),
  );
  const monthlyTotal = stats.strategiesLastMonthByDay.reduce(
    (sum, item) => sum + item.count,
    0,
  );
  const animatedMonthlyTotal = useCountUp(monthlyTotal, {
    enabled: chartInView,
  });

  return (
    <section className="strategy-dashboard" aria-label="Дашборд системи">
      <header className="strategy-dashboard__head">
        <div className="strategy-dashboard__head-text">
          <p className="strategy-dashboard__eyebrow">Реєстр стратегій</p>
          <h2 className="strategy-dashboard__title">Огляд бази даних</h2>
          <p className="strategy-dashboard__subtitle muted">
            Актуальна статистика з реєстру стратегій розвитку територіальних
            громад та областей України
          </p>
        </div>
      </header>

      <section
        ref={panelRef}
        className={`system-stats-panel${panelInView ? " system-stats-panel--visible" : ""}`}
        aria-label="Ключові показники"
      >
        <div className="system-stats-panel__featured">
          <StatTile
            label="Областей у базі"
            value={stats.regionsCount}
            hint="адміністративних одиниць верхнього рівня"
            animate={panelInView}
            featured
            tone="accent"
            delayMs={0}
          />
          <StatTile
            label="Громад у базі"
            value={stats.communitiesCount}
            hint="територіальних громад України"
            animate={panelInView}
            featured
            tone="default"
            delayMs={80}
          />
        </div>

        <div className="system-stats-panel__grid">
          <StatTile
            label="Усього стратегій"
            value={stats.totalStrategiesCount}
            hint="завантажених програм розвитку"
            animate={panelInView}
            delayMs={160}
          />
          <StatTile
            label="Заповнення бази"
            value={stats.communitiesWithStrategiesPercent}
            hint={`${formatCount(stats.communitiesWithStrategiesCount)} з ${formatCount(stats.communitiesCount)} громад мають завантажені стратегії`}
            animate={panelInView}
            decimals={1}
            percentBar={stats.communitiesWithStrategiesPercent}
            valueSuffix="%"
            delayMs={240}
          />
          <StatTile
            label="Середня к-сть стратегій"
            value={stats.averageStrategiesPerCommunity}
            hint="на одну громаду"
            animate={panelInView}
            decimals={stats.averageStrategiesPerCommunity % 1 === 0 ? 0 : 2}
            delayMs={320}
          />
          <StatTile
            label="Громади з веб-сайтом"
            value={stats.communitiesWithWebsiteCount}
            hint={`${stats.communitiesWithWebsitePercent.toLocaleString("uk-UA", {
              minimumFractionDigits: 0,
              maximumFractionDigits: 1,
            })}% від усіх громад`}
            animate={panelInView}
            percentBar={stats.communitiesWithWebsitePercent}
            delayMs={400}
          />
        </div>
      </section>

      <article
        ref={chartRef}
        className={`metric-card metric-card--chart${chartInView ? " metric-card--chart-visible" : ""}`}
      >
        <p className="metric-card__label">Додано програм за останній місяць</p>
        <p className="metric-card__value">{formatCount(animatedMonthlyTotal)}</p>
        <p className="metric-card__hint">за останні 30 днів, по днях</p>

        <div
          className="system-dashboard__chart"
          role="img"
          aria-label="Стовпчастий графік доданих програм за останній місяць"
        >
          {stats.strategiesLastMonthByDay.map((item, index) => {
            const barHeight =
              item.count === 0
                ? 0
                : Math.max(6, (item.count / maxDailyCount) * CHART_HEIGHT_PX);

            return (
              <div
                key={item.date}
                className="system-dashboard__chart-col"
                style={{ animationDelay: `${index * 35}ms` }}
              >
                <div className="system-dashboard__chart-bar-area">
                  <div
                    className={`system-dashboard__chart-bar${item.count === 0 ? " system-dashboard__chart-bar--empty" : ""}`}
                    style={
                      {
                        "--bar-target-height": `${barHeight}px`,
                        animationDelay: `${index * 35 + 120}ms`,
                      } as CSSProperties
                    }
                    title={`${formatDayLabel(item.date)}: ${item.count}`}
                  />
                </div>
                <span className="system-dashboard__chart-count">
                  {item.count > 0 ? item.count : ""}
                </span>
                <span className="system-dashboard__chart-label">
                  {formatDayLabel(item.date)}
                </span>
              </div>
            );
          })}
        </div>
      </article>
    </section>
  );
}
