import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Container } from "../../components/layout/Container";
import { SystemDashboard } from "../../components/dashboard/SystemDashboard";
import { fetchSystemStats, type SystemStats } from "../../lib/systemStats";
import "./HomePage.css";

export function HomePage() {
  const [stats, setStats] = useState<SystemStats | null>(null);
  const [statsLoading, setStatsLoading] = useState(true);
  const [statsError, setStatsError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    fetchSystemStats()
      .then((data) => {
        if (!cancelled) setStats(data);
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setStatsError(
            err instanceof Error
              ? err.message
              : "Не вдалося завантажити статистику",
          );
        }
      })
      .finally(() => {
        if (!cancelled) setStatsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <main className="home">
      <section className="home-hero">
        <Container className="home-hero__inner">
          <div className="home-hero__content">
            <h1 className="home-hero__title">Вітаємо</h1>
            <p className="home-hero__text">
              «Є рішення» допомагає знаходити та переглядати стратегії розвитку
              міст у зручному вигляді — з посиланням на документ і офіційне
              джерело.
            </p>
            <div className="home-hero__actions">
              <Link className="btn btn--primary" to="/search">
                Перейти до пошуку
              </Link>
              <a className="btn btn--tonal home-hero__dashboard-link" href="#dashboard">
                Переглянути статистику
              </a>
            </div>
          </div>
        </Container>
      </section>

      <section id="dashboard" className="home-section home-section--alt">
        <Container>
          {statsLoading && (
            <p className="home-dashboard__status muted">
              Завантаження статистики…
            </p>
          )}
          {statsError && (
            <p className="home-dashboard__error" role="alert">
              {statsError}
            </p>
          )}
          {stats && <SystemDashboard stats={stats} />}
        </Container>
      </section>

      <section id="about" className="home-section">
        <Container>
          <h2>Про сайт</h2>
          <p className="muted">
            Сервіс збирає стратегії та плани розвитку територіальних громад і
            областей.
          </p>
        </Container>
      </section>
    </main>
  );
}
