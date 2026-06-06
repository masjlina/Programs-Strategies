const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ?? 'http://localhost:5257'

let strategiesCache = null
let unitsCache = null
const strategyDetailsCache = new Map()

function normalize(value) {
  return String(value ?? '').trim().toLowerCase()
}

async function apiGet(path) {
  const response = await fetch(`${API_BASE_URL}${path}`)

  if (!response.ok) {
    throw new Error(`API request failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

function extractPeriod(title) {
  const match = title?.match(/(20\d{2})\s*[–-]\s*(20\d{2})/)
  if (!match) return ''
  return `${match[1]}–${match[2]}`
}

function getStatus(period) {
  const endYear = Number(period?.match(/20\d{2}\s*[–-]\s*(20\d{2})/)?.[1])
  if (!endYear) return 'active'
  return endYear < new Date().getFullYear() ? 'archive' : 'active'
}

function getCityFromUnitName(name) {
  return name
    ?.replace(/\s+міська територіальна громада$/i, '')
    ?.replace(/\s+територіальна громада$/i, '')
    ?.trim() || 'Невідома громада'
}

function normalizeTask(task) {
  return {
    ...task,
    label: task.label ?? String(task.number ?? ''),
    description: task.description ?? '',
  }
}

function normalizeOperationalGoal(goal) {
  const tasks = goal.tasks ?? goal.programTasks ?? []

  return {
    ...goal,
    label: goal.label ?? String(goal.number ?? ''),
    title: goal.title ?? '',
    tasks: tasks.map(normalizeTask),
    programTasks: tasks,
  }
}

function normalizeStrategicGoal(goal) {
  const operationalGoals = goal.operational_goals ?? goal.operationalGoals ?? []

  return {
    ...goal,
    label: goal.label ?? String(goal.number ?? ''),
    title: goal.title ?? '',
    operational_goals: operationalGoals.map(normalizeOperationalGoal),
    operationalGoals,
  }
}

function normalizeStrategy(strategy) {
  const strategicGoals = strategy.strategic_goals ?? strategy.strategicGoals ?? []

  return {
    ...strategy,
    title: strategy.title ?? '',
    strategic_goals: strategicGoals.map(normalizeStrategicGoal),
    strategicGoals,
  }
}

function getDirections(strategy) {
  const directions = strategy.strategic_goals
    .flatMap((strategicGoal) => strategicGoal.operational_goals)
    .map((operationalGoal) => operationalGoal.title)
    .filter(Boolean)

  return [...new Set(directions)]
}

function buildCatalogEntry(strategy, unit) {
  const normalizedStrategy = normalizeStrategy(strategy)
  const period = extractPeriod(normalizedStrategy.title)
  const directions = getDirections(normalizedStrategy)
  const unitId = normalizedStrategy.communityId ?? normalizedStrategy.districtId ?? normalizedStrategy.regionId

  return {
    id: normalizedStrategy.id,
    city: getCityFromUnitName(unit?.name),
    unitId: unitId,
    strategyId: normalizedStrategy.id,
    title: normalizedStrategy.title,
    period,
    summary: unit?.name
      ? `Стратегічний документ громади: ${unit.name}.`
      : 'Стратегічний документ громади.',
    directions,
    status: getStatus(period),
    fileUrl: '#',
    officialSourceUrl: '#',
  }
}

async function getUnitsMap() {
  if (!unitsCache) {
    unitsCache = Promise.all([
      apiGet('/api/Regions'),
      apiGet('/api/Districts'),
      apiGet('/api/Communities'),
    ]).then(([regions, districts, communities]) => {
      const allUnits = []
      regions.forEach(r => allUnits.push({ id: r.id, name: r.nameFull || r.name, type: 'Region' }))
      districts.forEach(d => allUnits.push({ id: d.id, name: d.nameFull || d.name, type: 'District' }))
      communities.forEach(c => allUnits.push({ id: c.id, name: c.nameFull || c.name, type: 'Community' }))
      return allUnits
    })
  }

  const units = await unitsCache
  return new Map(units.map((unit) => [unit.id, unit]))
}

function getStrategyById(id) {
  if (!strategyDetailsCache.has(id)) {
    strategyDetailsCache.set(id, apiGet(`/api/Strategies/${id}`))
  }

  return strategyDetailsCache.get(id)
}

async function getCatalog() {
  if (!strategiesCache) {
    strategiesCache = Promise.all([
      apiGet('/api/Strategies'),
      getUnitsMap(),
    ]).then(async ([strategies, unitsById]) => {
      const fullStrategies = await Promise.all(
        strategies.map((strategy) =>
          getStrategyById(strategy.id).catch(() => strategy),
        ),
      )

      return fullStrategies.map((strategy) => {
        const unitId = strategy.communityId ?? strategy.districtId ?? strategy.regionId
        return buildCatalogEntry(strategy, unitsById.get(unitId))
      })
    })
  }

  return strategiesCache
}

export async function getCities() {
  const catalog = await getCatalog()
  const cities = [...new Set(catalog.map((item) => item.city))]
  return cities.sort((a, b) => a.localeCompare(b, 'uk'))
}

export async function getDirectionsList() {
  const catalog = await getCatalog()
  const all = catalog.flatMap((item) => item.directions ?? [])
  return [...new Set(all)].sort((a, b) => a.localeCompare(b, 'uk'))
}

export { getDirectionsList as getDirections }

export async function searchStrategies(query) {
  const q = normalize(query)
  if (!q) return []

  const catalog = await getCatalog()

  return catalog.filter((item) => {
    const haystack = [
      item.city,
      item.title,
      item.summary,
      item.period,
      ...(item.directions ?? []),
    ]
      .join(' ')
      .toLowerCase()

    return haystack.includes(q)
  })
}

export async function getCatalogEntryById(id) {
  const catalog = await getCatalog()
  return catalog.find((item) => item.id === id) ?? null
}

export async function getStrategiesByCity(city) {
  if (!city) return []
  const catalog = await getCatalog()
  return catalog.filter((item) => item.city === city)
}

export async function loadAdministrativeUnit(unitId) {
  const unitsById = await getUnitsMap()
  const unit = unitsById.get(unitId)

  if (!unit) {
    throw new Error(`Administrative unit not found: ${unitId}`)
  }

  return { administrative_unit: unit }
}

export function getStrategyFromUnit(unitPayload, strategyId) {
  const strategies = unitPayload?.administrative_unit?.strategies
  if (!strategies) return null
  return strategies.find((strategy) => strategy.id === strategyId) ?? null
}

export async function loadStrategyForCatalogEntry(catalogEntry) {
  const [strategyPayload, unitsById] = await Promise.all([
    getStrategyById(catalogEntry.strategyId ?? catalogEntry.id),
    getUnitsMap(),
  ])

  const strategy = normalizeStrategy(strategyPayload)
  const unit = unitsById.get(strategy.administrativeUnitId ?? catalogEntry.unitId)

  return {
    unit,
    strategy,
  }
}
