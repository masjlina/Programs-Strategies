import { useState, useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Container } from "../../components/layout/Container";
import {
  buildUploadLink,
  getUnitTypeLabel,
  searchUnits,
  fetchReferenceData,
  type SearchItem,
} from "../../lib/api";
import { useAuth } from "../../context/AuthContext";
import "./SearchPage.css";

interface Region {
  id: string;
  name: string;
  nameFull?: string;
}

interface District {
  id: string;
  name: string;
  nameFull?: string;
  regionId: string;
}

type SelectedFilter = "all" | "community" | "region";
type SelectedSort = "programs-desc" | "programs-asc" | "name-asc";

export function SearchPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [items, setItems] = useState<SearchItem[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [loading, setLoading] = useState<boolean>(true);
  const [loadingMore, setLoadingMore] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState<string>("");
  const [debouncedQuery, setDebouncedQuery] = useState<string>("");
  const [selectedSort, setSelectedSort] =
    useState<SelectedSort>("programs-desc");
  const [selectedFilter, setSelectedFilter] = useState<SelectedFilter>("all");

  const [regions, setRegions] = useState<Region[]>([]);
  const [districts, setDistricts] = useState<District[]>([]);
  const [selectedRegionId, setSelectedRegionId] = useState<string>("");
  const [selectedDistrictId, setSelectedDistrictId] = useState<string>("");

  // Load reference data on mount
  useEffect(() => {
    fetchReferenceData()
      .then((data) => {
        setRegions(data.regions || []);
        setDistricts(data.districts || []);
      })
      .catch((err) => {
        console.error("Failed to load reference data", err);
      });
  }, []);

  // Filter districts based on selected region
  const filteredDistricts = selectedRegionId
    ? districts.filter((d) => d.regionId === selectedRegionId)
    : [];

  const handleRegionChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setSelectedRegionId(e.target.value);
    setSelectedDistrictId("");
  };

  const handleFilterChange = (filter: SelectedFilter) => {
    if (
      filter === "all" &&
      selectedFilter === "community" &&
      selectedRegionId &&
      selectedDistrictId
    ) {
      setSelectedSort("programs-desc");
      setSelectedRegionId("");
      setSelectedDistrictId("");
    }
    setSelectedFilter(filter);
    if (filter === "region") {
      setSelectedRegionId("");
      setSelectedDistrictId("");
    }
  };

  // Debounce the search query to avoid excessive API requests
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedQuery(searchQuery);
    }, 300);

    return () => {
      clearTimeout(handler);
    };
  }, [searchQuery]);

  // Load page 1 when query, filter, sort, region, or district changes
  useEffect(() => {
    let cancelled = false;
    setCurrentPage(1);

    (async () => {
      setLoading(true);
      setError(null);
      try {
        const result = await searchUnits(
          debouncedQuery,
          selectedFilter,
          selectedSort,
          1,
          30,
          selectedRegionId || undefined,
          selectedDistrictId || undefined
        );
        if (cancelled) return;
        setItems(result.items);
        setTotalCount(result.totalCount);
      } catch (err) {
        if (cancelled) return;
        setError(
          "Не вдалося завантажити дані з сервера. Будь ласка, спробуйте пізніше."
        );
        console.error(err);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [debouncedQuery, selectedFilter, selectedSort, selectedRegionId, selectedDistrictId]);

  // Load next pages when clicking "Show More"
  const loadMore = async () => {
    const nextPage = currentPage + 1;
    setLoadingMore(true);
    try {
      const result = await searchUnits(
        debouncedQuery,
        selectedFilter,
        selectedSort,
        nextPage,
        30,
        selectedRegionId || undefined,
        selectedDistrictId || undefined
      );
      setItems((prev) => [...prev, ...result.items]);
      setCurrentPage(nextPage);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError("Не вдалося завантажити додаткові дані.");
      console.error(err);
    } finally {
      setLoadingMore(false);
    }
  };

  return (
    <main className="search-hub">
      <Container>
        <header className="search-hub__hero">
          <h1 className="search-hub__title">Перелік стратегічних документів</h1>
          <p className="search-hub__subtitle muted">
            Шукайте громади та області для перегляду їхніх програм
            розвитку або додавайте нові.
          </p>
        </header>

        <section
          className="search-hub__controls-card"
          aria-label="Параметри пошуку"
        >
          <div className="search-bar-wrapper">
            <input
              className="search-hub__input-new"
              type="text"
              value={searchQuery}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setSearchQuery(e.target.value)
              }
              placeholder="Введіть назву громади, області чи тему програми (наприклад: Освіта)..."
              aria-label="Пошук територіальних одиниць"
            />
            {searchQuery && (
              <button
                type="button"
                className="search-clear-btn"
                onClick={() => setSearchQuery("")}
                aria-label="Очистити пошук"
              >
                ✕
              </button>
            )}
          </div>

          <div className="controls-row">
            <div className="filter-group">
              <span className="controls-label">Тип одиниці:</span>
              <div className="segmented-control">
                <button
                  type="button"
                  className={`segmented-btn ${selectedFilter === "all" ? "active" : ""}`}
                  onClick={() => handleFilterChange("all")}
                >
                  Всі
                </button>
                <button
                  type="button"
                  className={`segmented-btn ${selectedFilter === "community" ? "active" : ""}`}
                  onClick={() => handleFilterChange("community")}
                >
                  Громади
                </button>
                <button
                  type="button"
                  className={`segmented-btn ${selectedFilter === "region" ? "active" : ""}`}
                  onClick={() => handleFilterChange("region")}
                >
                  Області
                </button>
              </div>
            </div>

            {selectedFilter !== "region" && (
              <>
                <div className="filter-group">
                  <label className="controls-label" htmlFor="region-select">
                    Область:
                  </label>
                  <select
                    id="region-select"
                    className="filter-select"
                    value={selectedRegionId}
                    onChange={handleRegionChange}
                  >
                    <option value="">Всі</option>
                    {regions.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.nameFull || r.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="filter-group">
                  <label className="controls-label" htmlFor="district-select">
                    Район:
                  </label>
                  <select
                    id="district-select"
                    className="filter-select"
                    value={selectedDistrictId}
                    onChange={(e) => setSelectedDistrictId(e.target.value)}
                    disabled={!selectedRegionId}
                  >
                    <option value="">Всі</option>
                    {filteredDistricts.map((d) => (
                      <option key={d.id} value={d.id}>
                        {d.nameFull || d.name}
                      </option>
                    ))}
                  </select>
                </div>
              </>
            )}

            <div className="sort-group">
              <label className="controls-label" htmlFor="sort-select">
                Сортування:
              </label>
              <select
                id="sort-select"
                className="sort-select"
                value={selectedSort}
                onChange={(e: React.ChangeEvent<HTMLSelectElement>) =>
                  setSelectedSort(e.target.value as SelectedSort)
                }
              >
                <option value="programs-desc">
                  Програми (від більшої к-сті)
                </option>
                <option value="programs-asc">
                  Програми (від меншої к-сті)
                </option>
                <option value="name-asc">Назва (А-Я)</option>
              </select>
            </div>
          </div>
        </section>

        {loading ? (
          <div className="search-hub__status">Завантаження бази даних…</div>
        ) : error ? (
          <div className="search-hub__error" role="alert">
            {error}
          </div>
        ) : (
          <>
            <p className="search-results-info muted">
              Знайдено територіальних одиниць:{" "}
              <strong>{totalCount}</strong>
            </p>

            {items.length === 0 ? (
              <div className="search-hub__empty">
                Нічого не знайдено за вашим запитом. Спробуйте змінити ключові
                слова.
              </div>
            ) : (
              <div className="search-hub__cards-new">
                {items.map((item) => (
                  <article
                    key={`${item.type}-${item.id}`}
                    className={`unit-card ${item.strategies.length === 1 ? "unit-card--clickable" : ""}`}
                    onClick={
                      item.strategies.length === 1
                        ? (e) => {
                            const target = e.target as HTMLElement;
                            if (target.closest("a, button")) {
                              return;
                            }
                            navigate(`/strategies/${item.strategies[0].id}`);
                          }
                        : undefined
                    }
                  >
                    <div className="unit-card__header">
                      <span
                        className={`unit-card__badge unit-card__badge--${item.type.toLowerCase()}`}
                      >
                        {getUnitTypeLabel(item.type)}
                      </span>
                      {item.strategies.length > 0 ? (
                        <span className="unit-card__status unit-card__status--has-program">
                          Програм: {item.strategies.length}
                        </span>
                      ) : (
                        <span className="unit-card__status unit-card__status--no-program">
                          Немає програм
                        </span>
                      )}
                    </div>

                    <h2 className="unit-card__title">{item.name}</h2>

                    {item.type !== "Region" && (
                      <p className="unit-card__location muted">
                        {item.type === "Community" &&
                          item.districtName &&
                          `${item.districtName}, `}
                        {item.regionName}
                      </p>
                    )}

                    <div className="unit-card__content">
                      {item.strategies.length > 0 ? (
                        <div className="unit-card__strategies">
                          <p className="unit-card__strategies-label">
                            Документи розвитку:
                          </p>
                          <ul className="unit-card__strategies-list">
                            {item.strategies.map((s) => (
                              <li
                                key={s.id}
                                className="unit-card__strategy-item"
                              >
                                <Link
                                  className="unit-card__strategy-link"
                                  to={`/strategies/${s.id}`}
                                >
                                  <span className="unit-card__strategy-icon">
                                    📄
                                  </span>
                                  <span className="unit-card__strategy-title">
                                    {s.title}
                                  </span>
                                </Link>
                              </li>
                            ))}
                          </ul>
                        </div>
                      ) : (
                        <div className="unit-card__empty-state">
                          <p className="muted small-text">
                            Документи ще не завантажені для цієї одиниці.
                          </p>
                        </div>
                      )}
                    </div>

                    {user && (
                      <div className="unit-card__footer">
                        <Link
                          className={`btn ${item.strategies.length > 0 ? "btn--tonal" : "btn--primary"} btn--sm unit-card__action-btn`}
                          to={buildUploadLink({
                            type: item.type,
                            regionId: item.regionId ?? "",
                            districtId: item.districtId ?? "",
                            communityId: item.communityId ?? "",
                          })}
                        >
                          {item.strategies.length > 0
                            ? "Додати ще одну програму"
                            : "Додати програму"}
                        </Link>
                      </div>
                    )}
                  </article>
                ))}
              </div>
            )}

            {items.length < totalCount && (
              <div className="search-hub__more-container">
                <button
                  type="button"
                  className="btn btn--tonal load-more-btn"
                  onClick={loadMore}
                  disabled={loadingMore}
                >
                  {loadingMore ? "Завантаження..." : "Показати ще"}
                </button>
              </div>
            )}
          </>
        )}
      </Container>
    </main>
  );
}
