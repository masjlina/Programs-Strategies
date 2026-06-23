import { useCountUp } from "../../hooks/useCountUp";

interface StatTileProps {
  label: string;
  value: number;
  hint: string;
  animate: boolean;
  decimals?: number;
  featured?: boolean;
  tone?: "default" | "accent" | "muted";
  percentBar?: number;
  delayMs?: number;
  valueSuffix?: string;
}

function formatDisplay(value: number, decimals: number): string {
  return value.toLocaleString("uk-UA", {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  });
}

export function StatTile({
  label,
  value,
  hint,
  animate,
  decimals = 0,
  featured = false,
  tone = "default",
  percentBar,
  delayMs = 0,
  valueSuffix = "",
}: StatTileProps) {
  const animatedValue = useCountUp(value, { decimals, enabled: animate });
  const animatedPercent = useCountUp(percentBar ?? 0, {
    decimals: 1,
    enabled: animate && percentBar !== undefined,
  });

  return (
    <article
      className={`stat-tile${featured ? " stat-tile--featured" : ""} stat-tile--${tone}${animate ? " stat-tile--animate" : ""}`}
      style={{ animationDelay: `${delayMs}ms` }}
    >
      <p className="stat-tile__label">{label}</p>
      <p className="stat-tile__value">
        {formatDisplay(animatedValue, decimals)}
        {valueSuffix}
      </p>
      {percentBar !== undefined && (
        <div className="stat-tile__bar" aria-hidden="true">
          <div
            className="stat-tile__bar-fill"
            style={{ width: animate ? `${animatedPercent}%` : "0%" }}
          />
        </div>
      )}
      <p className="stat-tile__hint">{hint}</p>
    </article>
  );
}
