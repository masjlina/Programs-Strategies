import React, { useState } from 'react';
import { Search, AlertTriangle, TrendingUp, Layers } from 'lucide-react';
import { ResponsiveContainer, BarChart, CartesianGrid, XAxis, YAxis, Tooltip, ReferenceLine, Bar } from 'recharts';
import { apiGet } from '../../lib/api';
import { Container } from '../../components/layout/Container';
import './AnalyticsPage.css';

interface KeywordIntensityItemDto {
  strategyId: string;
  targetName: string;
  level: string;
  count: number;
  deviation: number;
}

interface KeywordStatisticsDto {
  globalMean: number;
  variance: number;
}

interface KeywordIntensityDto {
  keyword: string;
  statistics: KeywordStatisticsDto;
  items: KeywordIntensityItemDto[];
}

export function AnalyticsPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [activeKeyword, setActiveKeyword] = useState('');
  const [analyticsData, setAnalyticsData] = useState<KeywordIntensityDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchAnalytics = async (keywordToSearch: string) => {
    const trimmed = keywordToSearch.trim();
    if (!trimmed) {
      setError('Будь ласка, введіть ключове слово.');
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      // API call to the C# Web API analytics controller
      const result = await apiGet<KeywordIntensityDto>(`/api/analytics/intensity?keyword=${encodeURIComponent(trimmed)}`);
      setAnalyticsData(result);
      setActiveKeyword(result.keyword || trimmed);
    } catch (err: any) {
      console.error(err);
      setError(err?.message || 'Не вдалося завантажити аналітику для цього ключового слова.');
      setAnalyticsData(null);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    fetchAnalytics(searchQuery);
  };

  const handleSuggestionClick = (word: string) => {
    setSearchQuery(word);
    fetchAnalytics(word);
  };

  const getLevelLabel = (level: string) => {
    switch (level) {
      case 'Community': return 'Громада';
      case 'District': return 'Район';
      case 'Region': return 'Область';
      default: return level;
    }
  };

  const renderDeviationBadge = (deviation: number) => {
    const formatted = deviation > 0 ? `+${deviation}` : `${deviation}`;

    if (deviation > 10) {
      return (
        <span className="deviation-badge deviation-badge--anomaly">
          {formatted} (Високий фокус / Аномалія)
        </span>
      );
    } else if (deviation > 0) {
      return (
        <span className="deviation-badge deviation-badge--positive">
          {formatted} (Вище середнього)
        </span>
      );
    } else if (deviation < 0) {
      return (
        <span className="deviation-badge deviation-badge--negative">
          {formatted} (Нижче середнього)
        </span>
      );
    } else {
      return (
        <span className="deviation-badge deviation-badge--neutral">
          0.0 (На рівні середнього)
        </span>
      );
    }
  };

  // Custom Recharts Tooltip for elegant rendering
  const CustomChartTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      const data = payload[0].payload as KeywordIntensityItemDto;
      return (
        <div className="custom-chart-tooltip">
          <div className="custom-chart-tooltip__title">{data.targetName}</div>
          <div className="custom-chart-tooltip__row">
            <span>Рівень:</span>
            <span className="custom-chart-tooltip__value">{getLevelLabel(data.level)}</span>
          </div>
          <div className="custom-chart-tooltip__row">
            <span>Згадок:</span>
            <span className="custom-chart-tooltip__value custom-chart-tooltip__value--indigo">
              {data.count} {data.count === 1 ? 'раз' : data.count > 1 && data.count < 5 ? 'рази' : 'разів'}
            </span>
          </div>
          <div className="custom-chart-tooltip__row">
            <span>Відхилення:</span>
            <span className={`custom-chart-tooltip__value ${data.deviation >= 0 ? 'text-green' : 'text-red'}`}>
              {data.deviation > 0 ? `+${data.deviation}` : data.deviation}
            </span>
          </div>
        </div>
      );
    }
    return null;
  };

  return (
    <main className="analytics-page">
      <Container>
        <header className="analytics-page__header">
          <h1 className="analytics-page__title">Аналіз інтенсивності ключових слів</h1>
          <p className="analytics-page__subtitle">
            Порівняння частоти згадувань слів у регіональних стратегіях розвитку порівняно з середнім показником по країні
          </p>
        </header>

        {/* Top Search & KPIs section */}
        <div className="analytics-page__top-section">
          {/* Search Card */}
          <div className="analytics-search-card">
            <span className="analytics-search-card__label">Пошук за ключовим словом</span>
            <form onSubmit={handleSearchSubmit} className="analytics-search-card__form">
              <div className="analytics-search-card__input-wrapper">
                <Search className="analytics-search-card__icon" />
                <input
                  type="text"
                  className="analytics-search-card__input"
                  placeholder="Введіть слово (наприклад: громада, розвиток)..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  disabled={isLoading}
                />
              </div>
              <button type="submit" className="btn btn--primary" disabled={isLoading}>
                Шукати
              </button>
            </form>
          </div>

          {/* KPI Summary Cards */}
          <div className="analytics-kpis-grid">
            <div className="kpi-card">
              <div className="kpi-card__icon-wrapper">
                <TrendingUp className="kpi-card__icon" />
              </div>
              <div className="kpi-card__content">
                <span className="kpi-card__title">Середнє по країні</span>
                <span className="kpi-card__value">
                  {analyticsData ? analyticsData.statistics.globalMean.toFixed(2) : '0.00'}
                </span>
                <span className="kpi-card__trend">очікуване число на документ</span>
              </div>
            </div>

            <div className="kpi-card">
              <div className="kpi-card__icon-wrapper">
                <Layers className="kpi-card__icon" />
              </div>
              <div className="kpi-card__content">
                <span className="kpi-card__title">Дисперсія системи</span>
                <span className="kpi-card__value">
                  {analyticsData ? analyticsData.statistics.variance.toFixed(2) : '0.00'}
                </span>
                <span className="kpi-card__trend">міра розсіювання частот</span>
              </div>
            </div>
          </div>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="analytics-error">
            <AlertTriangle className="analytics-error__icon" />
            <p>{error}</p>
          </div>
        )}

        {/* Content displays based on state */}
        {isLoading && (
          <div className="analytics-loading">
            <div className="loading-spinner"></div>
            <p className="muted">Завантаження аналітичних даних...</p>
          </div>
        )}

        {!isLoading && !analyticsData && !error && (
          <div className="analytics-empty-state">
            <div className="analytics-empty-state__icon-wrapper">
              <Search className="analytics-empty-state__icon" />
            </div>
            <h3 className="analytics-empty-state__title">Немає результатів для відображення</h3>
            <p className="analytics-empty-state__text">
              Введіть ключове слово у пошуку вище, щоб проаналізувати інтенсивність згадок у текстах стратегій.
              Система автоматично лематизує слова на базі українського словника.
            </p>
            <div className="analytics-empty-state__suggestions">
              <span className="muted" style={{ display: 'block', width: '100%', marginBottom: '8px', fontSize: '0.85rem' }}>
                Спробуйте популярні запити:
              </span>
              <button className="suggestion-tag" onClick={() => handleSuggestionClick('громада')}>громада</button>
              <button className="suggestion-tag" onClick={() => handleSuggestionClick('забезпечення')}>забезпечення</button>
              <button className="suggestion-tag" onClick={() => handleSuggestionClick('розвиток')}>розвиток</button>
              <button className="suggestion-tag" onClick={() => handleSuggestionClick('агропереробка')}>агропереробка</button>
              <button className="suggestion-tag" onClick={() => handleSuggestionClick('інфраструктура')}>інфраструктура</button>
            </div>
          </div>
        )}

        {!isLoading && analyticsData && (
          <>
            {/* Chart Container */}
            <div className="analytics-dashboard-card">
              <div className="analytics-dashboard-card__header">
                <h2 className="analytics-dashboard-card__title">
                  Кількість згадувань у стратегіях (Інтенсивність для «{activeKeyword}»)
                </h2>
                <div className="analytics-dashboard-card__legend">
                  <div className="legend-item">
                    <span className="legend-dot legend-dot--indigo"></span>
                    <span>Кількість згадок</span>
                  </div>
                  <div className="legend-item">
                    <span className="legend-line--red"></span>
                    <span>Середнє по країні ({analyticsData.statistics.globalMean.toFixed(2)})</span>
                  </div>
                </div>
              </div>

              <div style={{ width: '100%', height: 340 }}>
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart
                    data={analyticsData.items}
                    margin={{ top: 10, right: 10, left: -20, bottom: 5 }}
                  >
                    <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="var(--border)" />
                    <XAxis
                      dataKey="targetName"
                      tickLine={false}
                      axisLine={false}
                      stroke="var(--text-muted)"
                      fontSize={11}
                      dy={8}
                    />
                    <YAxis
                      tickLine={false}
                      axisLine={false}
                      stroke="var(--text-muted)"
                      fontSize={11}
                      dx={-5}
                    />
                    <Tooltip content={<CustomChartTooltip />} cursor={{ fill: 'rgba(249, 115, 22, 0.04)' }} />
                    <ReferenceLine
                      y={analyticsData.statistics.globalMean}
                      stroke="#ef4444"
                      strokeDasharray="5 5"
                    />
                    <Bar
                      dataKey="count"
                      fill="#4f46e5"
                      radius={[4, 4, 0, 0]}
                      maxBarSize={50}
                    />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </div>

            {/* Data Grid (Table) */}
            <div className="analytics-table-card">
              <div className="analytics-table-card__header">
                <h2 className="analytics-table-card__title">
                  Результати схожості та фокусу (Таблиця громад)
                </h2>
              </div>
              <div className="table-wrapper">
                <table className="analytics-table">
                  <thead>
                    <tr>
                      <th>Громада / Регіон</th>
                      <th>Рівень</th>
                      <th>Кількість згадок</th>
                      <th>Відхилення від середнього</th>
                    </tr>
                  </thead>
                  <tbody>
                    {analyticsData.items.map((item) => (
                      <tr key={item.strategyId}>
                        <td className="cell-strategy-name">{item.targetName}</td>
                        <td>
                          <span className={`cell-strategy-level cell-strategy-level--${item.level.toLowerCase()}`}>
                            {getLevelLabel(item.level)}
                          </span>
                        </td>
                        <td>
                          {item.count} {item.count === 1 ? 'раз' : item.count > 1 && item.count < 5 ? 'рази' : 'разів'}
                        </td>
                        <td>
                          {renderDeviationBadge(item.deviation)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </>
        )}
      </Container>
    </main>
  );
}
