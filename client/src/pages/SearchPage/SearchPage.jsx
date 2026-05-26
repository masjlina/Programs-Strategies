import { useCallback, useEffect, useMemo, useState } from 'react'
import { Container } from '../../components/layout/Container.jsx'
import { StrategyResultCard } from '../../components/search/StrategyResultCard.jsx'
import { StrategyDetailPanel } from '../../components/search/StrategyDetailPanel.jsx'
import {
  loadStrategyForCatalogEntry,
  searchStrategies,
} from '../../lib/strategies.js'
import './SearchPage.css'

export function SearchPage() {
  const [query, setQuery] = useState('')
  const [submittedQuery, setSubmittedQuery] = useState('')
  const [selectedId, setSelectedId] = useState(null)
  const [loaded, setLoaded] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [previews, setPreviews] = useState({})

  const results = useMemo(
    () => (submittedQuery ? searchStrategies(submittedQuery) : []),
    [submittedQuery],
  )

  const selectedEntry = useMemo(
    () => results.find((item) => item.id === selectedId) ?? null,
    [results, selectedId],
  )

  const handleSearch = (e) => {
    e.preventDefault()
    setSubmittedQuery(query.trim())
    setSelectedId(null)
    setLoaded(null)
    setError(null)
    setPreviews({})
  }

  const loadPreview = useCallback(async (entry) => {
    const data = await loadStrategyForCatalogEntry(entry)
    setPreviews((prev) => {
      if (prev[entry.id]) return prev
      return { ...prev, [entry.id]: data.strategy }
    })
  }, [])

  useEffect(() => {
    if (!results.length) return
    for (const entry of results) {
      loadPreview(entry).catch(() => {})
    }
  }, [results, loadPreview])

  useEffect(() => {
    if (!selectedEntry) {
      setLoaded(null)
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    loadStrategyForCatalogEntry(selectedEntry)
      .then((data) => {
        if (!cancelled) setLoaded(data)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [selectedEntry])

  return (
    <main className="search-hub">
      <Container>
        <header className="search-hub__hero">
          <h1 className="search-hub__title">Перелік стратегічних документів</h1>
          <p className="search-hub__subtitle muted">
            Знайдіть програми розвитку вашої громади за містом або напрямком
          </p>
        </header>

        <form className="search-hub__form" onSubmit={handleSearch}>
          <input
            className="search-hub__input"
            type="search"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Введіть місто (Львів, Київ…) або напрям (Освіта, Економіка…)"
            aria-label="Пошук за містом або напрямком"
          />
          <button className="btn btn--primary search-hub__submit" type="submit">
            Пошук
          </button>
        </form>

        {!submittedQuery && (
          <p className="search-hub__hint muted">
            Наприклад: <button type="button" className="search-hub__example" onClick={() => { setQuery('Львів'); setSubmittedQuery('Львів') }}>Львів</button>
            {' · '}
            <button type="button" className="search-hub__example" onClick={() => { setQuery('Освіта'); setSubmittedQuery('Освіта') }}>Освіта</button>
            {' · '}
            <button type="button" className="search-hub__example" onClick={() => { setQuery('Івано-Франківськ'); setSubmittedQuery('Івано-Франківськ') }}>Івано-Франківськ</button>
          </p>
        )}

        {submittedQuery && results.length === 0 && (
          <p className="search-hub__empty" role="status">
            За запитом «{submittedQuery}» нічого не знайдено.
          </p>
        )}

        {results.length > 0 && (
          <section className="search-hub__results" aria-label="Результати пошуку">
            <div className="search-hub__cards">
              {results.map((entry) => (
                <StrategyResultCard
                  key={entry.id}
                  entry={entry}
                  strategy={previews[entry.id]}
                  isSelected={selectedId === entry.id}
                  onSelect={() =>
                    setSelectedId((current) =>
                      current === entry.id ? null : entry.id,
                    )
                  }
                />
              ))}
            </div>
          </section>
        )}

        {selectedEntry && (
          <section
            className="search-hub__detail"
            aria-label="Деталі обраної стратегії"
          >
            <StrategyDetailPanel
              catalogEntry={selectedEntry}
              loaded={loaded}
              loading={loading}
              error={error}
            />
          </section>
        )}
      </Container>
    </main>
  )
}
