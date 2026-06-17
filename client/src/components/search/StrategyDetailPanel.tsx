import { useMemo } from "react";
import {
  computeDashboardMetrics,
} from "../../lib/strategyMetrics";
import { StrategyGoalsTree } from "./StrategyGoalsTree";
import type { CatalogEntry, StrategyResponse } from "../../lib/strategies";

interface StrategyDetailPanelProps {
  catalogEntry: CatalogEntry;
  loaded: StrategyResponse | null;
  loading: boolean;
  error: string | null;
}

export function StrategyDetailPanel({
  catalogEntry,
  loaded,
  loading,
  error,
}: StrategyDetailPanelProps) {
  const strategy = loaded?.strategy;
  const unitName = loaded?.unit?.name;

  const metrics = useMemo(() => {
    if (!strategy || !catalogEntry) return null;
    return computeDashboardMetrics(strategy as any, catalogEntry);
  }, [strategy, catalogEntry]);

  const sourceLink =
    catalogEntry.officialSourceUrl ?? strategy?.strategyUrl ?? null;
  const fileLink = catalogEntry.fileUrl;
  const fileDisabled = !fileLink;
  const sourceDisabled = !sourceLink;

  if (loading) {
    return (
      <p className="strategy-detail__loading muted">Завантаження стратегії…</p>
    );
  }

  if (error) {
    return <p className="strategy-detail__error">{error}</p>;
  }

  if (!strategy || !metrics) return null;

  return (
    <div className="strategy-detail">
      <header className="strategy-detail__header">
        <div>
          <p className="strategy-detail__city">{catalogEntry.city}</p>
          {unitName && (
            <p className="muted strategy-detail__unit">{unitName}</p>
          )}
          <h2 className="strategy-detail__title">{strategy.title}</h2>
        </div>
        <div className="strategy-detail__actions">
          {fileDisabled ? (
            <span className="btn btn--outline btn--disabled">
              Завантажити PDF (скоро)
            </span>
          ) : (
            <a
              className="btn btn--outline"
              href={fileLink}
              target="_blank"
              rel="noreferrer"
            >
              Завантажити PDF
            </a>
          )}
          {!sourceDisabled && (
            <a
              className="btn btn--ghost"
              href={sourceLink}
              target="_blank"
              rel="noreferrer"
            >
              Офіційне джерело
            </a>
          )}
        </div>
      </header>

      {/*<StrategyDashboard metrics={metrics} directionLabel={directionLabel} />*/}
      {/*<StrategyMeasuresTable rows={measureRows} />*/}
      <StrategyGoalsTree strategy={strategy as any} />
    </div>
  );
}
