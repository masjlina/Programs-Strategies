const DEFAULT_API_BASE_URL = 'http://localhost:5257'

export function getApiBaseUrl() {
  const configured = import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '')
  return configured || DEFAULT_API_BASE_URL
}

export async function apiGet(path) {
  const response = await fetch(`${getApiBaseUrl()}${path}`)

  if (!response.ok) {
    const payload = await response.json().catch(() => null)
    throw new Error(payload?.message ?? `API request failed: ${response.status}`)
  }

  return response.json()
}

export async function apiPost(path, body) {
  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })

  if (!response.ok) {
    const payload = await response.json().catch(() => null)
    throw new Error(payload?.message ?? `API request failed: ${response.status}`)
  }

  return response.json()
}

export async function fetchReferenceData() {
  const [regions, districts, communities, strategies] = await Promise.all([
    apiGet('/api/Regions'),
    apiGet('/api/Districts'),
    apiGet('/api/Communities'),
    apiGet('/api/Strategies'),
  ])

  return { regions, districts, communities, strategies }
}

const UNIT_TYPE_LABELS = {
  Community: 'Громада',
  District: 'Район',
  Region: 'Область',
}

export function getUnitTypeLabel(type) {
  return UNIT_TYPE_LABELS[type] ?? type
}

export function buildUploadLink(item) {
  const params = new URLSearchParams({
    type: item.type,
    regionId: item.regionId,
  })

  if (item.type === 'District' || item.type === 'Community') {
    if (item.districtId) params.set('districtId', item.districtId)
  }

  if (item.type === 'Community' && item.communityId) {
    params.set('communityId', item.communityId)
  }

  return `/upload?${params}`
}
