interface DashboardMetrics {
  budgetTotalMln: number;
  budgetUsedMln: number;
  budgetUsedPercent: number;
  tasksDone: number;
  tasksInProgress: number;
  tasksTotal: number;
  executionPercent: number;
  periodElapsed: number;
  periodLabel: string;
  periodYearsPassed: number | null;
  periodYearsTotal: number | null;
  strategicGoals: number;
  operationalGoals: number;
  partnersCount: number;
  directionsCount: number;
  kpiPercent: number;
}

interface StrategyDashboardProps {
  metrics: DashboardMetrics;
  directionLabel?: string;
}

export function StrategyDashboard({
  metrics,
  directionLabel,
}: StrategyDashboardProps) {
  if (!metrics) {
    return null;
  }

  return (
    <section className="strategy-dashboard" aria-label="Дашборд стратегії">
      <div className="strategy-dashboard__head">
        <div>
          <h2 className="strategy-dashboard__title">
            {directionLabel ?? "Структура та прогрес"}
          </h2>
          <p className="strategy-dashboard__subtitle muted">
            На основі даних стратегічного документа
          </p>
        </div>
      </div>

      <div className="strategy-dashboard__grid">
        <article className="metric-card">
          <p className="metric-card__label">Завдань виконано</p>
          <p className="metric-card__value">
            {metrics.tasksDone} / {metrics.tasksTotal}
          </p>
          <div className="metric-card__bar">
            <div
              className="metric-card__bar-fill"
              style={{ width: `${metrics.executionPercent}%` }}
            />
          </div>
          <p className="metric-card__hint">
            у процесі: {metrics.tasksInProgress}
          </p>
        </article>

        <article className="metric-card">
          <p className="metric-card__label">Прогрес періоду</p>
          <p className="metric-card__value">{metrics.periodElapsed}%</p>
          <div className="metric-card__bar metric-card__bar--muted">
            <div
              className="metric-card__bar-fill"
              style={{ width: `${metrics.periodElapsed}%` }}
            />
          </div>
          <p className="metric-card__hint">
            {metrics.periodLabel}
            {metrics.periodYearsPassed != null &&
              ` · ${metrics.periodYearsPassed} з ${metrics.periodYearsTotal} років`}
          </p>
        </article>

        <article className="metric-card">
          <p className="metric-card__label">Структура програми</p>
          <p className="metric-card__value">
            {metrics.strategicGoals} / {metrics.operationalGoals}
          </p>
          <p className="metric-card__hint">стратегічних / оперативних цілей</p>
        </article>

        <article className="metric-card">
          <p className="metric-card__label">Напрямків у програмі</p>
          <p className="metric-card__value">{metrics.directionsCount}</p>
          <p className="metric-card__hint">тематичних блоків</p>
        </article>
      </div>
    </section>
  );
}
