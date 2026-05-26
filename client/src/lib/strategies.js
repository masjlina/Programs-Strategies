import catalog from '../data/strategiesCatalog.json'

const unitModules = import.meta.glob('../data/administrative_units/*.json')

function normalize(value) {
  return value.trim().toLowerCase()
}

export function getCities() {
  const cities = [...new Set(catalog.map((item) => item.city))]
  return cities.sort((a, b) => a.localeCompare(b, 'uk'))
}

export function getDirections() {
  const all = catalog.flatMap((item) => item.directions ?? [])
  return [...new Set(all)].sort((a, b) => a.localeCompare(b, 'uk'))
}

export function searchStrategies(query) {
  const q = normalize(query)
  if (!q) return []

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

export function getCatalogEntryById(id) {
  return catalog.find((item) => item.id === id) ?? null
}

export function getStrategiesByCity(city) {
  if (!city) return []
  return catalog.filter((item) => item.city === city)
}

export async function loadAdministrativeUnit(unitDataFile) {
  const loader = unitModules[`../data/administrative_units/${unitDataFile}.json`]
  if (!loader) {
    throw new Error(`Administrative unit file not found: ${unitDataFile}`)
  }
  const module = await loader()
  return module.default
}

export function getStrategyFromUnit(unitPayload, strategyId) {
  const unit = unitPayload?.administrative_unit
  if (!unit?.strategies) return null
  return unit.strategies.find((s) => s.id === strategyId) ?? null
}

export async function loadStrategyForCatalogEntry(catalogEntry) {
  const unitPayload = await loadAdministrativeUnit(catalogEntry.unitDataFile)
  const strategy = getStrategyFromUnit(unitPayload, catalogEntry.strategyId)
  if (!strategy) {
    throw new Error(`Strategy not found: ${catalogEntry.strategyId}`)
  }
  return {
    unit: unitPayload.administrative_unit,
    strategy,
  }
}
