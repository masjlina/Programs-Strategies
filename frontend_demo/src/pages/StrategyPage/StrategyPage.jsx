import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import CollapsibleBlock from '../../components/CollapsibleBlock/CollapsibleBlock.jsx'
import { Container } from '../../components/layout/Container.jsx'
import {
  getCatalogEntryById,
  loadStrategyForCatalogEntry,
} from '../../lib/strategies.js'
import './StrategyPage.css'

function StrategyContent({ strategy }) {
  return (
    <>
      {strategy.strategic_goals.map((strategicGoal) => (
        <CollapsibleBlock
          key={strategicGoal.id}
          title={`Стратегічна ціль ${strategicGoal.label}. ${strategicGoal.title}`}
          defaultOpen={false}
        >
          {strategicGoal.operational_goals.map((operationalGoal) => (
            <div
              key={operationalGoal.id}
              className="strategy-page__operational-goal"
            >
              <CollapsibleBlock
                title={`Оперативна ціль ${operationalGoal.label}. ${operationalGoal.title}`}
              >
                {operationalGoal.tasks.map((task) => (
                  <div key={task.id} className="strategy-page__task">
                    <CollapsibleBlock title={`Завдання ${task.label}`}>
                      <p className="strategy-page__task-description">
                        {task.description}
                      </p>
                    </CollapsibleBlock>
                  </div>
                ))}
              </CollapsibleBlock>
            </div>
          ))}
        </CollapsibleBlock>
      ))}
    </>
  )
}

export function StrategyPage() {
  const { id } = useParams()
  const catalogEntry = getCatalogEntryById(id ?? '')

  const [loaded, setLoaded] = useState(null)
  const [error, setError] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!catalogEntry) {
      setLoading(false)
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    loadStrategyForCatalogEntry(catalogEntry)
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
  }, [catalogEntry])

  if (!catalogEntry) {
    return (
      <main className="strategy-page strategy-page--centered">
        <Container>
          <h1>Стратегію не знайдено</h1>
          <p className="muted">Перевірте посилання або поверніться до пошуку.</p>
          <Link className="btn btn--tonal" to="/search">
            До пошуку
          </Link>
        </Container>
      </main>
    )
  }

  const fileLink = catalogEntry.fileUrl
  const sourceLink = catalogEntry.officialSourceUrl
  const fileDisabled = !fileLink || fileLink === '#'
  const sourceDisabled = !sourceLink || sourceLink === '#'

  const strategy = loaded?.strategy
  const unitName = loaded?.unit?.name

  return (
    <main className="strategy-page">
      <Container>
        <Link className="strategy-page__back" to="/search">
          ← Назад до пошуку
        </Link>

        <header className="strategy-page__header">
          <p className="strategy-page__city">{catalogEntry.city}</p>
          {unitName && (
            <p className="strategy-page__unit muted">{unitName}</p>
          )}
          <h1 className="strategy-page__title">
            {strategy?.title ?? catalogEntry.title}
          </h1>
          {catalogEntry.period && (
            <p className="strategy-page__period muted">Період: {catalogEntry.period}</p>
          )}
        </header>

        <div className="strategy-page__docs">
          <h2 className="strategy-page__docs-title">Документи</h2>
          <div className="strategy-page__docs-actions">
            {fileDisabled ? (
              <span className="btn btn--disabled" title="Файл ще не додано">
                Завантажити PDF (скоро)
              </span>
            ) : (
              <a
                className="btn btn--primary"
                href={fileLink}
                target="_blank"
                rel="noreferrer"
              >
                Завантажити PDF
              </a>
            )}
            {sourceDisabled ? (
              <span className="btn btn--disabled" title="Посилання ще не додано">
                Офіційне джерело (скоро)
              </span>
            ) : (
              <a
                className="btn btn--tonal"
                href={sourceLink}
                target="_blank"
                rel="noreferrer"
              >
                Офіційне джерело
              </a>
            )}
          </div>
        </div>

        {loading && <p className="muted">Завантаження стратегії…</p>}
        {error && <p className="strategy-page__error">{error}</p>}
        {strategy && <StrategyContent strategy={strategy} />}
      </Container>
    </main>
  )
}
