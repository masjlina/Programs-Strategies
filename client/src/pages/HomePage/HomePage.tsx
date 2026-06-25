import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Container } from "../../components/layout/Container";
import { SystemDashboard } from "../../components/dashboard/SystemDashboard";
import { fetchSystemStats, type SystemStats } from "../../lib/systemStats";
import { FileText, Search, BarChart3 } from "lucide-react";
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
          <div className="home-hero__image-container">
            <img
              src="/hero-illustration.png"
              alt="Ілюстрація стратегій розвитку міст"
              className="home-hero__image"
            />
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

      <section id="about" className="home-section home-about">
        <Container className="home-about__inner">
          <div className="home-about__info">
            <h2 className="home-about__title">Про проект</h2>
            <p className="home-about__description">
              Об'єднуємо громади навколо спільних рішень з сильними проектами. Зроблено в Україні для України. Разом будуємо
              ефективну спільноту розвитку.
            </p>
          </div>
          <div className="home-about__feature">
            <div className="home-about__icon-container">
              <FileText className="home-about__icon" />
            </div>
            <div className="home-about__text">
              <div className="home-about__feature-title">Єдина база документів</div>
              <div className="home-about__feature-desc">усі стратегії в одному місці</div>
            </div>
          </div>
          <div className="home-about__feature">
            <div className="home-about__icon-container">
              <Search className="home-about__icon" />
            </div>
            <div className="home-about__text">
              <div className="home-about__feature-title">Зручний пошук і фільтри</div>
              <div className="home-about__feature-desc">швидкий доступ до потрібної інформації</div>
            </div>
          </div>
          <div className="home-about__feature">
            <div className="home-about__icon-container">
              <BarChart3 className="home-about__icon" />
            </div>
            <div className="home-about__text">
              <div className="home-about__feature-title">Порівняння та аналітика</div>
              <div className="home-about__feature-desc">наочні дашборди для рішень</div>
            </div>
          </div>
        </Container>
      </section>
    </main>
  );
}
