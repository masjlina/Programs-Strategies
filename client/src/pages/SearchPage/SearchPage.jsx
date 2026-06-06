import { useState, useEffect, useMemo } from 'react'
import { Link } from 'react-router-dom'
import { Container } from '../../components/layout/Container.jsx'
import './SearchPage.css'

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ?? 'http://localhost:5257'

export function SearchPage() {
  const [regions, setRegions] = useState([])
  const [districts, setDistricts] = useState([])
  const [communities, setCommunities] = useState([])
  const [strategies, setStrategies] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // Search & Filter State
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedFilter, setSelectedFilter] = useState('all') // 'all', 'community', 'region'
  const [selectedSort, setSelectedSort] = useState('programs-desc') // 'programs-desc', 'programs-asc', 'name-asc'
  const [visibleCount, setVisibleCount] = useState(30)

  // Fetch all reference data & strategies
  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)

    Promise.all([
      fetch(`${API_BASE_URL}/api/Regions`).then((r) => r.json()),
      fetch(`${API_BASE_URL}/api/Districts`).then((r) => r.json()),
      fetch(`${API_BASE_URL}/api/Communities`).then((r) => r.json()),
      fetch(`${API_BASE_URL}/api/Strategies`).then((r) => r.json()),
    ])
      .then(([regionsData, districtsData, communitiesData, strategiesData]) => {
        if (!cancelled) {
          setRegions(regionsData || [])
          setDistricts(districtsData || [])
          setCommunities(communitiesData || [])
          setStrategies(strategiesData || [])
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError('Не вдалося завантажити дані з сервера. Будь ласка, спробуйте пізніше.')
          console.error(err)
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [])

  // Create lookups
  const regionsMap = useMemo(() => {
    const map = {}
    regions.forEach((r) => {
      map[r.id] = r.nameFull || r.name
    })
    return map
  }, [regions])

  const districtsMap = useMemo(() => {
    const map = {}
    districts.forEach((d) => {
      map[d.id] = d.nameFull || d.name
    })
    return map
  }, [districts])

  // Combine and normalize units to search over
  const searchList = useMemo(() => {
    const list = []

    // 1. Add Regions
    regions.forEach((r) => {
      list.push({
        id: r.id,
        name: r.nameFull || r.name,
        type: 'Region',
        regionId: r.id,
        districtId: null,
        regionName: r.nameFull || r.name,
        districtName: '',
        strategies: strategies.filter((s) => s.regionId === r.id),
      })
    })

    // 2. Add Communities
    communities.forEach((c) => {
      list.push({
        id: c.id,
        name: c.nameFull || c.name,
        type: 'Community',
        regionId: c.regionId,
        districtId: c.districtId,
        regionName: regionsMap[c.regionId] || '',
        districtName: districtsMap[c.districtId] || '',
        strategies: strategies.filter((s) => s.communityId === c.id),
      })
    })

    return list
  }, [regions, communities, strategies, regionsMap, districtsMap])

  // Reset pagination when search query or filter changes
  useEffect(() => {
    setVisibleCount(30)
  }, [searchQuery, selectedFilter, selectedSort])

  // Filter and sort matching results
  const filteredAndSortedList = useMemo(() => {
    let result = [...searchList]

    // A. Query filter
    if (searchQuery.trim() !== '') {
      const q = searchQuery.toLowerCase()
      result = result.filter((item) => {
        const nameMatch = item.name.toLowerCase().includes(q)
        const regionMatch = item.regionName.toLowerCase().includes(q)
        const districtMatch = item.districtName.toLowerCase().includes(q)
        const strategyMatch = item.strategies.some((s) =>
          s.title.toLowerCase().includes(q)
        )
        return nameMatch || regionMatch || districtMatch || strategyMatch
      })
    }

    // B. Category filter (All, Communities, Regions)
    if (selectedFilter === 'community') {
      result = result.filter((item) => item.type === 'Community')
    } else if (selectedFilter === 'region') {
      result = result.filter((item) => item.type === 'Region')
    }

    // C. Sorting
    result.sort((a, b) => {
      if (selectedSort === 'programs-desc') {
        const diff = b.strategies.length - a.strategies.length
        if (diff !== 0) return diff
        return a.name.localeCompare(b.name, 'uk') // Alphabetical tie-breaker
      } else if (selectedSort === 'programs-asc') {
        const diff = a.strategies.length - b.strategies.length
        if (diff !== 0) return diff
        return a.name.localeCompare(b.name, 'uk')
      } else if (selectedSort === 'name-asc') {
        return a.name.localeCompare(b.name, 'uk')
      }
      return 0
    })

    return result
  }, [searchList, searchQuery, selectedFilter, selectedSort])

  const visibleResults = useMemo(() => {
    return filteredAndSortedList.slice(0, visibleCount)
  }, [filteredAndSortedList, visibleCount])

  return (
    <main className="search-hub">
      <Container>
        <header className="search-hub__hero">
          <h1 className="search-hub__title">Перелік стратегічних документів</h1>
          <p className="search-hub__subtitle muted">
            Шукайте громади та області для перегляду їхніх програм розвитку або додавайте нові.
          </p>
        </header>

        {/* Search Input */}
        <section className="search-hub__controls-card" aria-label="Параметри пошуку">
          <div className="search-bar-wrapper">
            <input
              className="search-hub__input-new"
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Введіть назву громади, району, області чи тему програми (наприклад: Освіта)..."
              aria-label="Пошук територіальних одиниць"
            />
            {searchQuery && (
              <button
                type="button"
                className="search-clear-btn"
                onClick={() => setSearchQuery('')}
                aria-label="Очистити пошук"
              >
                ✕
              </button>
            )}
          </div>

          {/* Filters & Sorting */}
          <div className="controls-row">
            <div className="filter-group">
              <span className="controls-label">Тип одиниці:</span>
              <div className="segmented-control">
                <button
                  type="button"
                  className={`segmented-btn ${selectedFilter === 'all' ? 'active' : ''}`}
                  onClick={() => setSelectedFilter('all')}
                >
                  Всі
                </button>
                <button
                  type="button"
                  className={`segmented-btn ${selectedFilter === 'community' ? 'active' : ''}`}
                  onClick={() => setSelectedFilter('community')}
                >
                  Громади
                </button>
                <button
                  type="button"
                  className={`segmented-btn ${selectedFilter === 'region' ? 'active' : ''}`}
                  onClick={() => setSelectedFilter('region')}
                >
                  Області
                </button>
              </div>
            </div>

            <div className="sort-group">
              <label className="controls-label" htmlFor="sort-select">Сортування:</label>
              <select
                id="sort-select"
                className="sort-select"
                value={selectedSort}
                onChange={(e) => setSelectedSort(e.target.value)}
              >
                <option value="programs-desc">Програми (від більшої к-сті)</option>
                <option value="programs-asc">Програми (від меншої к-сті)</option>
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
            {/* Results count */}
            <p className="search-results-info muted">
              Знайдено територіальних одиниць: <strong>{filteredAndSortedList.length}</strong>
            </p>

            {filteredAndSortedList.length === 0 ? (
              <div className="search-hub__empty">
                Нічого не знайдено за вашим запитом. Спробуйте змінити ключові слова.
              </div>
            ) : (
              <div className="search-hub__cards-new">
                {visibleResults.map((item) => (
                  <article key={`${item.type}-${item.id}`} className="unit-card">
                    <div className="unit-card__header">
                      <span className={`unit-card__badge unit-card__badge--${item.type.toLowerCase()}`}>
                        {item.type === 'Community' ? 'Громада' : 'Область'}
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

                    {item.type === 'Community' && (
                      <p className="unit-card__location muted">
                        {item.districtName && `${item.districtName}, `}
                        {item.regionName}
                      </p>
                    )}

                    <div className="unit-card__content">
                      {item.strategies.length > 0 ? (
                        <div className="unit-card__strategies">
                          <p className="unit-card__strategies-label">Документи розвитку:</p>
                          <ul className="unit-card__strategies-list">
                            {item.strategies.map((s) => (
                              <li key={s.id} className="unit-card__strategy-item">
                                <Link className="unit-card__strategy-link" to={`/strategies/${s.id}`}>
                                  <span className="unit-card__strategy-icon">📄</span>
                                  <span className="unit-card__strategy-title">{s.title}</span>
                                </Link>
                              </li>
                            ))}
                          </ul>
                        </div>
                      ) : (
                        <div className="unit-card__empty-state">
                          <p className="muted small-text">Документи ще не завантажені для цієї одиниці.</p>
                        </div>
                      )}
                    </div>

                    <div className="unit-card__footer">
                      <Link
                        className={`btn ${item.strategies.length > 0 ? 'btn--tonal' : 'btn--primary'} btn--sm unit-card__action-btn`}
                        to={`/upload?type=${item.type}&regionId=${item.regionId}${
                          item.districtId ? `&districtId=${item.districtId}` : ''
                        }&communityId=${item.id}`}
                      >
                        {item.strategies.length > 0 ? '➕ Додати ще одну програму' : '➕ Додати програму'}
                      </Link>
                    </div>
                  </article>
                ))}
              </div>
            )}

            {filteredAndSortedList.length > visibleCount && (
              <div className="search-hub__more-container">
                <button
                  type="button"
                  className="btn btn--tonal load-more-btn"
                  onClick={() => setVisibleCount((prev) => prev + 30)}
                >
                  Показати ще
                </button>
              </div>
            )}
          </>
        )}
      </Container>
    </main>
  )
}
