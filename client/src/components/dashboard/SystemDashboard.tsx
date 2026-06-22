import type { SystemStats } from "../../lib/systemStats";

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
  const maxDailyCount = Math.max(
    1,
    ...stats.strategiesLastMonthByDay.map((item) => item.count),
  );
  const monthlyTotal = stats.strategiesLastMonthByDay.reduce(
    (sum, item) => sum + item.count,
    0,
  );

  return (
    <section className="strategy-dashboard" aria-label="Дашборд системи">
      <div className="strategy-dashboard__head">
        <div>
          <h2 className="strategy-dashboard__title">Огляд бази даних</h2>
          <p className="strategy-dashboard__subtitle muted">
            Актуальна статистика з реєстру стратегій розвитку
          </p>
        </div>
      </div>

      <div className="strategy-dashboard__grid">
        <article className="metric-card">
          <p className="metric-card__label">Областей у базі</p>
          <p className="metric-card__value">{formatCount(stats.regionsCount)}</p>
          <p className="metric-card__hint">адміністративних одиниць верхнього рівня</p>
        </article>

        <article className="metric-card">
          <p className="metric-card__label">Громад у базі</p>
          <p className="metric-card__value">
            {formatCount(stats.communitiesCount)}
          </p>
          <p className="metric-card__hint">територіальних громад України</p>
        </article>

        <article className="metric-card">
          <p className="metric-card__label">Усього стратегій</p>
          <p className="metric-card__value">
            {formatCount(stats.totalStrategiesCount)}
          </p>
          <p className="metric-card__hint">завантажених програм розвитку</p>
        </article>

        <article className="metric-card">
          <p className="metric-card__label">Громад без стратегій</p>
          <p className="metric-card__value">
            {formatCount(stats.communitiesWithoutStrategiesCount)}
          </p>
          <p className="metric-card__hint">
            з {formatCount(stats.communitiesCount)} громад ще без програм
          </p>
        </article>

        <article className="metric-card">
          <p className="metric-card__label">Середня к-сть стратегій</p>
          <p className="metric-card__value">
            {stats.averageStrategiesPerCommunity.toLocaleString("uk-UA", {
              minimumFractionDigits: 0,
              maximumFractionDigits: 2,
            })}
          </p>
          <p className="metric-card__hint">на одну громаду</p>
        </article>

        <article className="metric-card">
          <p className="metric-card__label">Громади з веб-сайтом</p>
          <p className="metric-card__value">
            {formatCount(stats.communitiesWithWebsiteCount)}
          </p>
          <div className="metric-card__bar">
            <div
              className="metric-card__bar-fill"
              style={{ width: `${stats.communitiesWithWebsitePercent}%` }}
            />
          </div>
          <p className="metric-card__hint">
            {stats.communitiesWithWebsitePercent.toLocaleString("uk-UA", {
              minimumFractionDigits: 0,
              maximumFractionDigits: 1,
            })}
            % від усіх громад
          </p>
        </article>
      </div>

      <article className="metric-card metric-card--chart">
        <p className="metric-card__label">Додано програм за останній місяць</p>
        <p className="metric-card__value">{formatCount(monthlyTotal)}</p>
        <p className="metric-card__hint">за останні 30 днів, по днях</p>

        <div
          className="system-dashboard__chart"
          role="img"
          aria-label="Стовпчастий графік доданих програм за останній місяць"
        >
          {stats.strategiesLastMonthByDay.map((item) => {
            const barHeight =
              item.count === 0
                ? 0
                : Math.max(6, (item.count / maxDailyCount) * CHART_HEIGHT_PX);

            return (
              <div key={item.date} className="system-dashboard__chart-col">
                <div className="system-dashboard__chart-bar-area">
                  <div
                    className={`system-dashboard__chart-bar${item.count === 0 ? " system-dashboard__chart-bar--empty" : ""}`}
                    style={{ height: `${barHeight}px` }}
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
