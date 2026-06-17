import { useState, useEffect, useMemo } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Container } from '../../components/layout/Container'
import { apiPost, fetchReferenceData, getUnitTypeLabel, apiUploadDocument } from '../../lib/api'
import './UploadPage.css'


// ── Types ────────────────────────────────────────────────────────────────────

interface ProgramTask {
  id: string
  label: string
  number: number
  description: string
  title?: string
}

interface OperationalGoal {
  id: string
  label: string
  number: number
  title: string
  programTasks: ProgramTask[]
  description?: string
}

interface StrategicGoal {
  id: string
  label: string
  number: number
  title: string
  operationalGoals: OperationalGoal[]
  description?: string
}

interface ParsedStrategy {
  title: string
  strategyUrl: string | null
  strategicGoals: StrategicGoal[]
}

interface Region {
  id: string
  name: string
  nameFull?: string
}

interface District {
  id: string
  name: string
  nameFull?: string
  regionId: string
}

interface Community {
  id: string
  name: string
  nameFull?: string
  regionId: string
  districtId: string
}

interface UnitListItem {
  id: string
  name: string
  type: UnitType
  regionId?: string
  districtId?: string
  communityId?: string
}

type UnitType = 'Region' | 'District' | 'Community'
type SaveStatus = 'idle' | 'success' | 'error'
type MoveDirection = 'up' | 'down'
type InsertType = 'strategic' | 'operational' | 'task'

interface EditFields {
  label: string
  title: string
  description: string
}

interface EditableGoalsTreeProps {
  strategy: ParsedStrategy

  onDelete: (id: string) => void
  onMove: (id: string, direction: MoveDirection) => void
  onInsert: (siblingId: string, type: InsertType) => void
  onAddStrategic: () => void
  onAddOperational: (goalId: string) => void
  onAddTask: (opId: string) => void
  editingId: string | null
  editFields: EditFields
  setEditFields: React.Dispatch<React.SetStateAction<EditFields>>
  startEdit: (item: { id: string; label?: string; title?: string; description?: string }) => void
  cancelEdit: () => void
  saveEdit: (id: string) => void
  collapsedIds: Set<string>
  setCollapsedIds: React.Dispatch<React.SetStateAction<Set<string>>>
}

// ── Helpers ──────────────────────────────────────────────────────────────────

// Helper to ensure all items in the preview tree have unique client-side IDs
function ensureIds(strategy: ParsedStrategy): ParsedStrategy {
  if (!strategy) return strategy
  return {
    ...strategy,
    strategicGoals: (strategy.strategicGoals || []).map((g, gi) => {
      const gId = g.id || `g-${Date.now()}-${gi}-${Math.random().toString(36).substr(2, 9)}`
      return {
        ...g,
        id: gId,
        operationalGoals: (g.operationalGoals || []).map((op, opi) => {
          const opId = op.id || `op-${Date.now()}-${gi}-${opi}-${Math.random().toString(36).substr(2, 9)}`
          return {
            ...op,
            id: opId,
            programTasks: (op.programTasks || []).map((t, ti) => {
              const tId = t.id || `t-${Date.now()}-${gi}-${opi}-${ti}-${Math.random().toString(36).substr(2, 9)}`
              return {
                ...t,
                id: tId
              }
            })
          }
        })
      }
    })
  }
}

// Resequence sequential numbers (number = index + 1) recursively for correct rendering order
function resequenceGoals(goals: StrategicGoal[]): StrategicGoal[] {
  return goals.map((g, gi) => ({
    ...g,
    number: gi + 1,
    operationalGoals: (g.operationalGoals || []).map((op, opi) => ({
      ...op,
      number: opi + 1,
      programTasks: (op.programTasks || []).map((t, ti) => ({
        ...t,
        number: ti + 1
      }))
    }))
  }))
}

// ── Inline SVG Icons ─────────────────────────────────────────────────────────

const EditIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2">
    <path strokeLinecap="round" strokeLinejoin="round" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
  </svg>
)

const DeleteIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2">
    <path strokeLinecap="round" strokeLinejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-4v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
  </svg>
)

const MoveUpIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2">
    <path strokeLinecap="round" strokeLinejoin="round" d="M5 15l7-7 7 7" />
  </svg>
)

const MoveDownIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2">
    <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
  </svg>
)

const CheckIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2">
    <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
  </svg>
)

const CancelIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2">
    <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
  </svg>
)

// ── Main Component ───────────────────────────────────────────────────────────

export function UploadPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  // App data state
  const [regions, setRegions] = useState<Region[]>([])
  const [districts, setDistricts] = useState<District[]>([])
  const [communities, setCommunities] = useState<Community[]>([])
  const [strategies, setStrategies] = useState<any[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Selection state
  const [selectedType, setSelectedType] = useState<UnitType>('Community')
  const [selectedRegionId, setSelectedRegionId] = useState('')
  const [selectedDistrictId, setSelectedDistrictId] = useState('')
  const [selectedCommunityId, setSelectedCommunityId] = useState('')

  // Search filter for list of units without strategies
  const [searchQuery, setSearchQuery] = useState('')
  const [activeTab, setActiveTab] = useState<UnitType>('Community')

  // File / Upload state
  const [dragActive, setDragActive] = useState(false)
  const [fileName, setFileName] = useState('')
  const [parsedStrategy, setParsedStrategy] = useState<ParsedStrategy | null>(null)
  const [jsonError, setJsonError] = useState<string | null>(null)
  const [parsing, setParsing] = useState(false)

  // Save state
  const [saving, setSaving] = useState(false)
  const [saveStatus, setSaveStatus] = useState<SaveStatus>('idle')
  const [saveMessage, setSaveMessage] = useState('')

  // Expanded/Collapsed nodes for preview tree
  const [collapsedIds, setCollapsedIds] = useState<Set<string>>(new Set())

  // Editing state for goals/tasks tree preview
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editFields, setEditFields] = useState<EditFields>({ label: '', title: '', description: '' })
  const [pendingNewItemId, setPendingNewItemId] = useState<string | null>(null)

  const discardPendingNewItem = (nextEditingId: string | null = null) => {
    if (pendingNewItemId && pendingNewItemId !== nextEditingId) {
      deleteItem(pendingNewItemId)
      setPendingNewItemId(null)
    }
  }

  const startEdit = (item: { id: string; label?: string; title?: string; description?: string }) => {
    discardPendingNewItem(item.id)
    setEditingId(item.id)
    setEditFields({
      label: item.label || '',
      title: item.title || '',
      description: item.description || ''
    })
  }

  const cancelEdit = () => {
    if (editingId && editingId === pendingNewItemId) {
      deleteItem(editingId)
      setPendingNewItemId(null)
    }
    setEditingId(null)
    setEditFields({ label: '', title: '', description: '' })
  }

  const saveEdit = (id: string) => {
    updateItem(id, editFields)
    if (id === pendingNewItemId) {
      setPendingNewItemId(null)
    }
    setEditingId(null)
    setEditFields({ label: '', title: '', description: '' })
  }

  const updateItem = (id: string, fields: EditFields) => {
    setParsedStrategy((prev) => {
      if (!prev) return prev
      const updatedGoals = prev.strategicGoals.map((g) => {
        if (g.id === id) {
          return { ...g, ...fields }
        }
        return {
          ...g,
          operationalGoals: g.operationalGoals.map((op) => {
            if (op.id === id) {
              return { ...op, ...fields }
            }
            return {
              ...op,
              programTasks: op.programTasks.map((t) => {
                if (t.id === id) {
                  return { ...t, ...fields }
                }
                return t
              })
            }
          })
        }
      })
      return {
        ...prev,
        strategicGoals: updatedGoals
      }
    })
  }

  const deleteItem = (id: string) => {
    setParsedStrategy((prev) => {
      if (!prev) return prev
      const filteredGoals = prev.strategicGoals
        .filter((g) => g.id !== id)
        .map((g) => ({
          ...g,
          operationalGoals: g.operationalGoals
            .filter((op) => op.id !== id)
            .map((op) => ({
              ...op,
              programTasks: op.programTasks.filter((t) => t.id !== id)
            }))
        }))
      return {
        ...prev,
        strategicGoals: resequenceGoals(filteredGoals)
      }
    })
  }

  const moveItem = (id: string, direction: MoveDirection) => {
    setParsedStrategy((prev) => {
      if (!prev) return prev

      const swapInArray = <T,>(arr: T[], index1: number, index2: number): T[] => {
        const copy = [...arr]
        const temp = copy[index1]
        copy[index1] = copy[index2]
        copy[index2] = temp
        return copy
      }

      let newGoals = prev.strategicGoals

      // 1. Strategic Goals
      const gIndex = prev.strategicGoals.findIndex((g) => g.id === id)
      if (gIndex !== -1) {
        const targetIndex = direction === 'up' ? gIndex - 1 : gIndex + 1
        if (targetIndex >= 0 && targetIndex < prev.strategicGoals.length) {
          newGoals = swapInArray(prev.strategicGoals, gIndex, targetIndex)
        }
      } else {
        // 2. Check operational goals and tasks
        newGoals = prev.strategicGoals.map((g) => {
          const opIndex = g.operationalGoals.findIndex((op) => op.id === id)
          if (opIndex !== -1) {
            const targetIndex = direction === 'up' ? opIndex - 1 : opIndex + 1
            if (targetIndex >= 0 && targetIndex < g.operationalGoals.length) {
              return {
                ...g,
                operationalGoals: swapInArray(g.operationalGoals, opIndex, targetIndex)
              }
            }
            return g
          }

          return {
            ...g,
            operationalGoals: g.operationalGoals.map((op) => {
              const tIndex = op.programTasks.findIndex((t) => t.id === id)
              if (tIndex !== -1) {
                const targetIndex = direction === 'up' ? tIndex - 1 : tIndex + 1
                if (targetIndex >= 0 && targetIndex < op.programTasks.length) {
                  return {
                    ...op,
                    programTasks: swapInArray(op.programTasks, tIndex, targetIndex)
                  }
                }
              }
              return op
            })
          }
        })
      }

      return {
        ...prev,
        strategicGoals: resequenceGoals(newGoals)
      }
    })
  }

  const insertAfter = (siblingId: string, type: InsertType) => {
    discardPendingNewItem()
    const newId = `${type === 'strategic' ? 'g' : type === 'operational' ? 'op' : 't'}-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`

    setParsedStrategy((prev) => {
      if (!prev) return prev

      const createNewItem = (itemType: InsertType, baseNumber: number, baseLabel?: string): any => {
        if (itemType === 'strategic') {
          return {
            id: newId,
            label: String(baseNumber),
            number: baseNumber,
            title: 'Нова стратегічна ціль',
            operationalGoals: []
          }
        }
        if (itemType === 'operational') {
          return {
            id: newId,
            label: baseLabel ? `${baseLabel}.${baseNumber}` : String(baseNumber),
            number: baseNumber,
            title: 'Нова оперативна ціль',
            programTasks: []
          }
        }
        return {
          id: newId,
          label: baseLabel ? `${baseLabel}.${baseNumber}` : String(baseNumber),
          number: baseNumber,
          description: 'Нове завдання'
        }
      }

      let newGoals = prev.strategicGoals

      // 1. Strategic Goals
      const gIndex = prev.strategicGoals.findIndex((g) => g.id === siblingId)
      if (gIndex !== -1) {
        const nextNum = prev.strategicGoals[gIndex].number + 1
        const newItem = createNewItem('strategic', nextNum)
        const nextGoals = [...prev.strategicGoals]
        nextGoals.splice(gIndex + 1, 0, newItem)
        newGoals = nextGoals
      } else {
        // 2. Operational Goals & Tasks
        newGoals = prev.strategicGoals.map((g) => {
          const opIndex = g.operationalGoals.findIndex((op) => op.id === siblingId)
          if (opIndex !== -1) {
            const nextNum = g.operationalGoals[opIndex].number + 1
            const newItem = createNewItem('operational', nextNum, g.label)
            const nextOps = [...g.operationalGoals]
            nextOps.splice(opIndex + 1, 0, newItem)
            return { ...g, operationalGoals: nextOps }
          }

          return {
            ...g,
            operationalGoals: g.operationalGoals.map((op) => {
              const tIndex = op.programTasks.findIndex((t) => t.id === siblingId)
              if (tIndex !== -1) {
                const nextNum = op.programTasks[tIndex].number + 1
                const newItem = createNewItem('task', nextNum, op.label)
                const nextTasks = [...op.programTasks]
                nextTasks.splice(tIndex + 1, 0, newItem)
                return { ...op, programTasks: nextTasks }
              }
              return op
            })
          }
        })
      }

      return {
        ...prev,
        strategicGoals: resequenceGoals(newGoals)
      }
    })

    // Auto start editing
    setPendingNewItemId(newId)
    setEditingId(newId)
    if (type === 'strategic') {
      setEditFields({ label: '', title: 'Нова стратегічна ціль', description: '' })
    } else if (type === 'operational') {
      setEditFields({ label: '', title: 'Нова оперативна ціль', description: '' })
    } else {
      setEditFields({ label: '', title: '', description: 'Нове завдання' })
    }
  }

  const addStrategicGoal = () => {
    discardPendingNewItem()
    const newId = `g-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
    setParsedStrategy((prev) => {
      if (!prev) return prev
      const nextNum = prev.strategicGoals.length + 1
      const newItem: StrategicGoal = {
        id: newId,
        label: String(nextNum),
        number: nextNum,
        title: 'Нова стратегічна ціль',
        operationalGoals: []
      }
      return {
        ...prev,
        strategicGoals: [...prev.strategicGoals, newItem]
      }
    })
    setPendingNewItemId(newId)
    setEditingId(newId)
    setEditFields({ label: '', title: 'Нова стратегічна ціль', description: '' })
  }

  const addOperationalGoal = (goalId: string) => {
    discardPendingNewItem()
    const newId = `op-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
    setParsedStrategy((prev) => {
      if (!prev) return prev
      return {
        ...prev,
        strategicGoals: prev.strategicGoals.map((g) => {
          if (g.id !== goalId) return g
          const nextNum = g.operationalGoals.length + 1
          const newItem: OperationalGoal = {
            id: newId,
            label: g.label ? `${g.label}.${nextNum}` : String(nextNum),
            number: nextNum,
            title: 'Нова оперативна ціль',
            programTasks: []
          }
          return {
            ...g,
            operationalGoals: [...g.operationalGoals, newItem]
          }
        })
      }
    })
    setPendingNewItemId(newId)
    setEditingId(newId)
    setEditFields({ label: '', title: 'Нова оперативна ціль', description: '' })
  }

  const addTask = (opId: string) => {
    discardPendingNewItem()
    const newId = `t-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
    setParsedStrategy((prev) => {
      if (!prev) return prev
      return {
        ...prev,
        strategicGoals: prev.strategicGoals.map((g) => {
          return {
            ...g,
            operationalGoals: g.operationalGoals.map((op) => {
              if (op.id !== opId) return op
              const nextNum = op.programTasks.length + 1
              const newItem: ProgramTask = {
                id: newId,
                label: op.label ? `${op.label}.${nextNum}` : String(nextNum),
                number: nextNum,
                description: 'Нове завдання'
              }
              return {
                ...op,
                programTasks: [...op.programTasks, newItem]
              }
            })
          }
        })
      }
    })
    setPendingNewItemId(newId)
    setEditingId(newId)
    setEditFields({ label: '', title: '', description: 'Нове завдання' })
  }

  // Fetch initial data
  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)

    fetchReferenceData()
      .then(({ regions: regionsData, districts: districtsData, communities: communitiesData, strategies: strategiesData }) => {
        if (!cancelled) {
          setRegions(regionsData || [])
          setDistricts(districtsData || [])
          setCommunities(communitiesData || [])
          setStrategies(strategiesData || [])

          const qType = searchParams.get('type')
          const qRegionId = searchParams.get('regionId')
          const qDistrictId = searchParams.get('districtId')
          const qCommunityId = searchParams.get('communityId')

          if (qType) setSelectedType(qType as UnitType)
          if (qRegionId) setSelectedRegionId(qRegionId)
          if (qDistrictId) setSelectedDistrictId(qDistrictId)
          if (qCommunityId) setSelectedCommunityId(qCommunityId)
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError('Не вдалося завантажити довідкові дані. Будь ласка, перевірте з\'єднання.')
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

  // Auto-select first region/district if none selected and type changes
  useEffect(() => {
    if (regions.length > 0 && !selectedRegionId) {
      setSelectedRegionId(regions[0].id)
    }
  }, [regions, selectedRegionId])

  // Filtered districts for selected region
  const filteredDistricts = useMemo(() => {
    if (!selectedRegionId) return []
    return districts.filter((d) => d.regionId === selectedRegionId)
  }, [districts, selectedRegionId])

  // Reset selected district when region changes
  const handleRegionChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const regId = e.target.value
    setSelectedRegionId(regId)
    const nextDistricts = districts.filter((d) => d.regionId === regId)
    if (nextDistricts.length > 0) {
      setSelectedDistrictId(nextDistricts[0].id)
      const nextComm = communities.filter((c) => c.districtId === nextDistricts[0].id)
      if (nextComm.length > 0) {
        setSelectedCommunityId(nextComm[0].id)
      } else {
        setSelectedCommunityId('')
      }
    } else {
      setSelectedDistrictId('')
      setSelectedCommunityId('')
    }
  }

  // Filtered communities for selected district
  const filteredCommunities = useMemo(() => {
    if (!selectedDistrictId) return []
    return communities.filter((c) => c.districtId === selectedDistrictId)
  }, [communities, selectedDistrictId])

  const handleDistrictChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const distId = e.target.value
    setSelectedDistrictId(distId)
    const nextComm = communities.filter((c) => c.districtId === distId)
    if (nextComm.length > 0) {
      setSelectedCommunityId(nextComm[0].id)
    } else {
      setSelectedCommunityId('')
    }
  }

  // Calculate units without strategies
  const unitsWithoutStrategies = useMemo<UnitListItem[]>(() => {
    const regionsWithStr = new Set(strategies.map((s: any) => s.regionId).filter(Boolean))
    const districtsWithStr = new Set(strategies.map((s: any) => s.districtId).filter(Boolean))
    const communitiesWithStr = new Set(strategies.map((s: any) => s.communityId).filter(Boolean))

    const list: UnitListItem[] = []

    // Regions without strategies
    regions.forEach((r) => {
      if (!regionsWithStr.has(r.id)) {
        list.push({ id: r.id, name: r.nameFull || r.name, type: 'Region', regionId: r.id })
      }
    })

    // Districts without strategies
    districts.forEach((d) => {
      if (!districtsWithStr.has(d.id)) {
        list.push({
          id: d.id,
          name: d.nameFull || d.name,
          type: 'District',
          regionId: d.regionId,
          districtId: d.id,
        })
      }
    })

    // Communities without strategies
    communities.forEach((c) => {
      if (!communitiesWithStr.has(c.id)) {
        list.push({
          id: c.id,
          name: c.nameFull || c.name,
          type: 'Community',
          regionId: c.regionId,
          districtId: c.districtId,
          communityId: c.id,
        })
      }
    })

    return list
  }, [regions, districts, communities, strategies])

  // Filter list by searchQuery and activeTab
  const filteredUnitsList = useMemo<UnitListItem[]>(() => {
    let list = unitsWithoutStrategies.filter((item) => item.type === activeTab)
    if (searchQuery.trim() !== '') {
      const q = searchQuery.toLowerCase()
      list = list.filter((item) => item.name.toLowerCase().includes(q))
    }
    return list.slice(0, 15) // Limit list display to first 15 for slickness
  }, [unitsWithoutStrategies, searchQuery, activeTab])

  // Quick click handler for list
  const handleQuickSelect = (item: UnitListItem) => {
    setSelectedType(item.type)
    if (item.regionId) setSelectedRegionId(item.regionId)
    if (item.districtId) setSelectedDistrictId(item.districtId)
    if (item.communityId) setSelectedCommunityId(item.communityId)
  }

  // Parse JSON file or upload/parse documents (DOCX, PDF, DOC) on the server
  const processUploadFile = async (file: File) => {
    if (!file) return

    setFileName(file.name)
    setJsonError(null)
    setParsedStrategy(null)
    setCollapsedIds(new Set())

    const isJson = file.name.endsWith('.json')

    if (isJson) {
      const reader = new FileReader()
      reader.onload = (event) => {
        try {
          const json = JSON.parse(event.target?.result as string)

          // Determine if this is a CommunityDto style json or StrategyDto style json
          let strategy: any = null

          // Style A: CommunityDto (e.g. data.json) with nested strategies array
          if (json.strategies && Array.isArray(json.strategies) && json.strategies.length > 0) {
            strategy = json.strategies[0]
          } else if (json.Strategies && Array.isArray(json.Strategies) && json.Strategies.length > 0) {
            strategy = json.Strategies[0]
          }
          // Style B: Direct StrategyDto
          else if (json.title || json.Title) {
            strategy = json
          }

          if (!strategy) {
            throw new Error('Файл не містить коректної структури програми розвитку (відсутній заголовок або перелік стратегій).')
          }

          // Normalize naming properties recursively to standard DTO structure (title, strategicGoals, operationalGoals, programTasks)
          const normalizeUploadStrategy = (raw: any): ParsedStrategy => {
            const title = raw.title || raw.Title || 'Програма розвитку'
            const strategyUrl = raw.strategyUrl || raw.StrategyUrl || null

            // Goals mapping
            const rawGoals = raw.strategicGoals || raw.StrategicGoals || raw.strategic_goals || []
            const strategicGoals: StrategicGoal[] = rawGoals.map((g: any, gi: number) => {
              const label = g.label || g.Label || String(gi + 1)
              const number = g.number || g.Number || (gi + 1)
              const gTitle = g.title || g.Title || 'Стратегічна ціль'

              // Operational goals
              const rawOps = g.operationalGoals || g.OperationalGoals || g.operational_goals || []
              const operationalGoals: OperationalGoal[] = rawOps.map((op: any, opi: number) => {
                const opLabel = op.label || op.Label || `${label}.${opi + 1}`
                const opNumber = op.number || op.Number || (opi + 1)
                const opTitle = op.title || op.Title || 'Операційна ціль'

                // Program tasks
                const rawTasks = op.programTasks || op.ProgramTasks || op.tasks || op.Tasks || []
                const programTasks: ProgramTask[] = rawTasks.map((t: any, ti: number) => {
                  const tLabel = t.label || t.Label || `${opLabel}.${ti + 1}`
                  const tNumber = t.number || t.Number || (ti + 1)
                  const description = t.description || t.Description || 'Завдання'
                  return { id: '', label: tLabel, number: tNumber, description }
                })

                return { id: '', label: opLabel, number: opNumber, title: opTitle, programTasks }
              })

              return { id: '', label, number, title: gTitle, operationalGoals }
            })

            return { title, strategyUrl, strategicGoals }
          }

          const normalized = normalizeUploadStrategy(strategy)
          setParsedStrategy(ensureIds(normalized))
        } catch (err: any) {
          setJsonError(err.message || 'Не вдалося розпарсити JSON-файл.')
          console.error(err)
        }
      }
      reader.onerror = () => {
        setJsonError('Помилка під час читання файлу.')
      }
      reader.readAsText(file)
    } else {
      // It's a document file (.docx, .pdf, .doc)
      setParsing(true)
      try {
        const parsed = await apiUploadDocument<ParsedStrategy>(file)
        if (parsed) {
          setParsedStrategy(ensureIds(parsed))
        } else {
          throw new Error('Отримано пустий результат від сервера.')
        }
      } catch (err: any) {
        setJsonError(err.message || 'Не вдалося розпізнати структуру документа.')
        console.error(err)
      } finally {
        setParsing(false)
      }
    }
  }

  // Handle Drag Events
  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true)
    } else if (e.type === 'dragleave') {
      setDragActive(false)
    }
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setDragActive(false)

    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      processUploadFile(e.dataTransfer.files[0])
    }
  }

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      processUploadFile(e.target.files[0])
    }
  }


  // Validate URL format on frontend
  const validateFrontendUrl = (url: string): boolean => {
    if (!url || url.trim() === '') return true
    try {
      const parsed = new URL(url)
      return parsed.protocol === 'http:' || parsed.protocol === 'https:'
    } catch (e) {
      return false
    }
  }

  // Submit parsed strategy
  const handleSave = async () => {
    if (!parsedStrategy) return

    if (!parsedStrategy.title?.trim()) {
      setSaveStatus('error')
      setSaveMessage('Назва програми не може бути порожньою.')
      return
    }

    if (parsedStrategy.strategyUrl && !validateFrontendUrl(parsedStrategy.strategyUrl)) {
      setSaveStatus('error')
      setSaveMessage('Некоректний формат URL для програми. URL має починатися з http:// або https://')
      return
    }

    let targetId: string | null = null
    if (selectedType === 'Region') {
      targetId = selectedRegionId
      if (!targetId) {
        setSaveStatus('error')
        setSaveMessage('Оберіть цільову область.')
        return
      }
    } else if (selectedType === 'District') {
      targetId = selectedDistrictId
      if (!targetId) {
        setSaveStatus('error')
        setSaveMessage('Оберіть цільовий район.')
        return
      }
    } else if (selectedType === 'Community') {
      targetId = selectedCommunityId
      if (!targetId) {
        setSaveStatus('error')
        setSaveMessage('Оберіть цільову громаду.')
        return
      }
    }

    setSaving(true)
    setSaveStatus('idle')
    setSaveMessage('')

    // Clean up temporary IDs from goals structure before sending to server
    const cleanStrategicGoals = (parsedStrategy.strategicGoals || []).map((g) => ({
      label: g.label,
      number: g.number,
      title: g.title,
      operationalGoals: (g.operationalGoals || []).map((op) => ({
        label: op.label,
        number: op.number,
        title: op.title,
        programTasks: (op.programTasks || []).map((t) => ({
          label: t.label,
          number: t.number,
          description: t.description
        }))
      }))
    }))

    // Prepare strategy payload
    const payload = {
      title: parsedStrategy.title,
      strategyUrl: parsedStrategy.strategyUrl,
      regionId: selectedType === 'Region' ? targetId : null,
      districtId: selectedType === 'District' ? targetId : null,
      communityId: selectedType === 'Community' ? targetId : null,
      strategicGoals: cleanStrategicGoals,
    }

    try {
      await apiPost('/api/Strategies', payload)

      setSaveStatus('success')
      setSaveMessage('Програму успішно додано та зв\u2019язано!')

      // Redirect to search after short delay
      setTimeout(() => {
        navigate('/search')
      }, 2000)
    } catch (err: any) {
      setSaveStatus('error')
      setSaveMessage(err.message || 'Виникла помилка під час надсилання запиту.')
      console.error(err)
    } finally {
      setSaving(false)
    }
  }

  const handleMockUpload = () => {
    const mockJson: ParsedStrategy = {
      title: "Тестова програма розвитку громади 2026-2030",
      strategyUrl: "https://example.com/strategy",
      strategicGoals: [
        {
          id: '',
          label: "1",
          number: 1,
          title: "Стратегічна ціль 1. Економічний розвиток",
          operationalGoals: [
            {
              id: '',
              label: "1.1",
              number: 1,
              title: "Операційна ціль 1.1. Підтримка малого бізнесу",
              programTasks: [
                {
                  id: '',
                  label: "1.1.1",
                  number: 1,
                  description: "Завдання 1.1.1. Створення бізнес-інкубатора"
                },
                {
                  id: '',
                  label: "1.1.2",
                  number: 2,
                  description: "Завдання 1.1.2. Організація щорічного бізнес-форуму"
                }
              ]
            }
          ]
        }
      ]
    }
    setParsedStrategy(ensureIds(mockJson))
    setFileName('test_strategy.json')
    setJsonError(null)
    setCollapsedIds(new Set())
  }

  // Get name of current selected unit for summary banner
  const selectedUnitName = useMemo(() => {
    if (selectedType === 'Region') {
      const unit = regions.find((r) => r.id === selectedRegionId)
      return unit ? unit.nameFull || unit.name : 'Не обрано'
    }
    if (selectedType === 'District') {
      const unit = districts.find((d) => d.id === selectedDistrictId)
      return unit ? unit.nameFull || unit.name : 'Не обрано'
    }
    const unit = communities.find((c) => c.id === selectedCommunityId)
    return unit ? unit.nameFull || unit.name : 'Не обрано'
  }, [selectedType, selectedRegionId, selectedDistrictId, selectedCommunityId, regions, districts, communities])

  const programContentsCard = parsedStrategy && (
    <section className="upload-grid__toc-card card-panel" aria-label="Зміст програми">
      <div className="preview-toc">
        <h2 className="panel-title">Зміст програми</h2>

        <div className="preview-toc__scrollable">
          <ol className="preview-toc__list">
            {(parsedStrategy.strategicGoals || []).map((goal) => (
              <li key={goal.id} className="preview-toc__item">
                <a
                  href={`#goal-${goal.id}`}
                  onClick={() => {
                    setCollapsedIds((prev) => {
                      const next = new Set(prev)
                      next.delete(goal.id)
                      return next
                    })
                  }}
                >
                  <span className="preview-toc__text">
                    <span className="preview-toc__prefix">
                      Стратегічна ціль {goal.label ? `${goal.label}.` : ''}
                    </span>{' '}
                    {goal.title || 'Без назви'}
                  </span>
                </a>
                {goal.operationalGoals && goal.operationalGoals.length > 0 && (
                  <ol className="preview-toc__sublist">
                    {goal.operationalGoals.map((op) => (
                      <li key={op.id} className="preview-toc__subitem">
                        <a
                          href={`#goal-${op.id}`}
                          onClick={() => {
                            setCollapsedIds((prev) => {
                              const next = new Set(prev)
                              next.delete(goal.id)
                              next.delete(op.id)
                              return next
                            })
                          }}
                        >
                          <span className="preview-toc__text">
                            {op.label ? `${op.label}. ` : ''}
                            {op.title || 'Без назви'}
                          </span>
                        </a>
                      </li>
                    ))}
                  </ol>
                )}
              </li>
            ))}
          </ol>
        </div>

        <div className="preview-toc__actions">
          <button
            type="button"
            className="btn btn--tonal btn--sm"
            onClick={() => {
              const ids: string[] = []
              parsedStrategy.strategicGoals.forEach((goal) => {
                ids.push(goal.id)
                if (goal.operationalGoals) {
                  goal.operationalGoals.forEach((op) => {
                    ids.push(op.id)
                  })
                }
              })
              setCollapsedIds(new Set(ids))
            }}
          >
            Згорнути все
          </button>
          <button
            type="button"
            className="btn btn--ghost btn--sm"
            onClick={() => setCollapsedIds(new Set())}
          >
            Розгорнути все
          </button>
        </div>

        <div className="preview-toc__footer">
          <button
            className="btn btn--primary btn--save-sticky"
            onClick={handleSave}
            disabled={saving || !parsedStrategy.title?.trim()}
          >
            {saving ? 'Збереження...' : 'Зберегти програму'}
          </button>
          {saveMessage && (
            <div className={`save-status-msg save-status-msg--${saveStatus} preview-toc__status-msg`}>
              {saveMessage}
            </div>
          )}
        </div>
      </div>
    </section>
  )

  return (
    <main className="upload-page">
      <Container>
        <header className="upload-page__hero">
          <h1 className="upload-page__title">Завантаження нових програм</h1>
          <p className="upload-page__subtitle muted">
            Додайте стратегічний документ розвитку до обраної області або територіальної громади
          </p>
        </header>

        {loading ? (
          <div className="upload-page__status">Завантаження довідкових даних…</div>
        ) : error ? (
          <div className="upload-page__error-banner" role="alert">
            {error}
          </div>
        ) : (
          <div className="upload-grid">
            {/* Left Column: List of units without programs */}
            <div className="upload-grid__sidebar-stack">
              <section className="upload-grid__sidebar card-panel" aria-label="Адміністративні одиниці без стратегій">
                <h2 className="panel-title">Одиниці без програм</h2>
                <p className="muted panel-description">
                  Оберіть із переліку територіальних одиниць, у яких наразі немає жодного завантаженого документа.
                </p>

                {/* Tabs */}
                <div className="tab-menu">
                  <button
                    className={`tab-menu__btn ${activeTab === 'Community' ? 'active' : ''}`}
                    onClick={() => setActiveTab('Community')}
                  >
                    Громади
                  </button>
                  <button
                    className={`tab-menu__btn ${activeTab === 'Region' ? 'active' : ''}`}
                    onClick={() => setActiveTab('Region')}
                  >
                    Області
                  </button>
                </div>

                {/* Search */}
                <input
                  className="sidebar-search"
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Пошук одиниці..."
                />

                {/* Units List */}
                <ul className="units-list">
                  {filteredUnitsList.length > 0 ? (
                    filteredUnitsList.map((item) => (
                      <li key={item.id} className="units-list__item">
                        <button
                          className="units-list__action-btn"
                          onClick={() => handleQuickSelect(item)}
                        >
                          <span className="unit-name">{item.name}</span>
                          <span className="unit-badge">{getUnitTypeLabel(activeTab)}</span>
                        </button>
                      </li>
                    ))
                  ) : (
                    <li className="units-list__empty muted">
                      {searchQuery ? 'Нічого не знайдено' : 'Усі одиниці в цій категорії мають програми!'}
                    </li>
                  )}
                </ul>
              </section>

              {programContentsCard}
            </div>

            {/* Right Column: Upload forms, target selection & preview */}
            <div className="upload-grid__main">
              {/* Card 1: Select affiliation */}
              <section className="card-panel" aria-label="Вибір належності до громади">
                <h2 className="panel-title">1. Вибір належності до громади</h2>
                <p className="muted panel-description">
                  Вкажіть адміністративно-територіальну одиницю, для якої призначена ця програма.
                </p>

                <div className="form-group">
                  <label className="form-label">Тип територіального рівня</label>
                  <div className="level-select-grid">
                    <label className={`level-radio-label ${selectedType === 'Community' ? 'active' : ''}`}>
                      <input
                        type="radio"
                        name="unitType"
                        value="Community"
                        checked={selectedType === 'Community'}
                        onChange={() => setSelectedType('Community')}
                      />
                      <span>Громада</span>
                    </label>
                    <label className={`level-radio-label ${selectedType === 'Region' ? 'active' : ''}`}>
                      <input
                        type="radio"
                        name="unitType"
                        value="Region"
                        checked={selectedType === 'Region'}
                        onChange={() => setSelectedType('Region')}
                      />
                      <span>Область</span>
                    </label>
                  </div>
                </div>

                <div className="dropdowns-grid">
                  {/* Always show Region */}
                  <div className="form-group">
                    <label className="form-label">Область</label>
                    <select
                      className="form-select"
                      value={selectedRegionId}
                      onChange={handleRegionChange}
                    >
                      <option value="" disabled>Оберіть область...</option>
                      {regions.map((r) => (
                        <option key={r.id} value={r.id}>
                          {r.nameFull || r.name}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Show District if District or Community selected */}
                  {(selectedType === 'District' || selectedType === 'Community') && (
                    <div className="form-group">
                      <label className="form-label">Район</label>
                      <select
                        className="form-select"
                        value={selectedDistrictId}
                        onChange={handleDistrictChange}
                        disabled={filteredDistricts.length === 0}
                      >
                        <option value="" disabled>Оберіть район...</option>
                        {filteredDistricts.map((d) => (
                          <option key={d.id} value={d.id}>
                            {d.nameFull || d.name}
                          </option>
                        ))}
                      </select>
                    </div>
                  )}

                  {/* Show Community if Community selected */}
                  {selectedType === 'Community' && (
                    <div className="form-group">
                      <label className="form-label">Громада</label>
                      <select
                        className="form-select"
                        value={selectedCommunityId}
                        onChange={(e) => setSelectedCommunityId(e.target.value)}
                        disabled={filteredCommunities.length === 0}
                      >
                        <option value="" disabled>Оберіть громаду...</option>
                        {filteredCommunities.map((c) => (
                          <option key={c.id} value={c.id}>
                            {c.nameFull || c.name}
                          </option>
                        ))}
                      </select>
                    </div>
                  )}
                </div>
              </section>

              {/* Card 2: JSON or Document file upload */}
              <section className="card-panel" aria-label="Завантаження файлу програми">
                <h2 className="panel-title">2. Завантаження програми</h2>
                <p className="muted panel-description">
                  Перетягніть готовий JSON-файл або документ (PDF, DOCX, DOC) чи оберіть його на комп'ютері.
                </p>

                <div
                  className={`dropzone ${dragActive ? 'active' : ''} ${parsedStrategy ? 'has-file' : ''} ${parsing ? 'parsing' : ''}`}
                  onDragEnter={handleDrag}
                  onDragOver={handleDrag}
                  onDragLeave={handleDrag}
                  onDrop={handleDrop}
                >
                  <input
                    type="file"
                    id="file-upload"
                    className="dropzone__input"
                    accept=".json,application/json,.docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document,.pdf,application/pdf,.doc,application/msword"
                    onChange={handleFileChange}
                    disabled={parsing}
                  />
                  {parsing ? (
                    <div className="parsing-status">
                      <div className="spinner"></div>
                      <span>Обробка та розпізнавання документа...</span>
                      <p className="muted">Це може зайняти кілька секунд</p>
                    </div>
                  ) : (
                    <label htmlFor="file-upload" className="dropzone__label">
                      <svg className="dropzone__icon" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                      </svg>
                      {fileName ? (
                        <div className="dropzone__status">
                          <span className="file-highlight">{fileName}</span>
                          <p className="muted">Перетягніть або натисніть сюди, щоб обрати інший файл</p>
                        </div>
                      ) : (
                        <div className="dropzone__status">
                          <span>Перетягніть сюди JSON або документ (PDF, DOCX, DOC)</span>
                          <p className="muted">або натисніть кнопку для пошуку на комп'ютері</p>
                        </div>
                      )}
                    </label>
                  )}
                </div>

                <button
                  id="mock-upload-test-strategy"
                  className="btn btn--tonal"
                  type="button"
                  onClick={handleMockUpload}
                  style={{ marginTop: '14px', width: '100%' }}
                >
                  Завантажити тестовий приклад (test_strategy.json)
                </button>

                {jsonError && (
                  <div className="error-message" role="alert">
                    <strong>Помилка валідації JSON:</strong> {jsonError}
                  </div>
                )}
              </section>

              {/* Card 3: Preview of parsing */}
              {parsedStrategy && (
                <section className="card-panel" aria-label="Попередній перегляд результату">
                  <h2 className="panel-title">3. Попередній перегляд результату парсингу</h2>
                  <p className="muted panel-description">
                    Перевірте правильність розпізнавання структури документа перед фіксацією в базі даних.
                  </p>

                  <div className="preview-layout-grid">
                    <div className="preview-layout__content">
                      <div className="preview-summary">
                        <div className="summary-item">
                          <span className="summary-label">Об'єкт прив'язки: </span>
                          <strong className="summary-value highlight-text">{selectedUnitName}</strong>
                        </div>
                      </div>

                      <div className="editable-section" style={{ marginTop: '16px', marginBottom: '20px' }}>
                        <div className="form-group">
                          <label className="form-label" htmlFor="upload-title-input">
                            Назва програми
                          </label>
                          <input
                            id="upload-title-input"
                            className="form-input"
                            type="text"
                            value={parsedStrategy.title}
                            onChange={(e) => setParsedStrategy(prev => prev ? ({ ...prev, title: e.target.value }) : prev)}
                            placeholder="Введіть назву програми розвитку"
                          />
                        </div>
                        <div className="form-group" style={{ marginTop: '12px' }}>
                          <label className="form-label" htmlFor="upload-url-input">
                            Посилання на оригінал програми (URL)
                          </label>
                          <input
                            id="upload-url-input"
                            className="form-input"
                            type="text"
                            value={parsedStrategy.strategyUrl || ''}
                            onChange={(e) => setParsedStrategy(prev => prev ? ({ ...prev, strategyUrl: e.target.value }) : prev)}
                            placeholder="Введіть URL, наприклад: https://example.com/strategy.pdf"
                          />
                        </div>
                      </div>
                      <div className="tree-container">
                        <div className="tree-header">Структура програми</div>
                        <div className="tree-body" style={{ maxHeight: 'none', overflowY: 'visible', padding: 0 }}>
                          {parsedStrategy.strategicGoals && parsedStrategy.strategicGoals.length > 0 ? (
                            <EditableGoalsTree
                              strategy={parsedStrategy}

                              onDelete={deleteItem}
                              onMove={moveItem}
                              onInsert={insertAfter}
                              onAddStrategic={addStrategicGoal}
                              onAddOperational={addOperationalGoal}
                              onAddTask={addTask}
                              editingId={editingId}
                              editFields={editFields}
                              setEditFields={setEditFields}
                              startEdit={startEdit}
                              cancelEdit={cancelEdit}
                              saveEdit={saveEdit}
                              collapsedIds={collapsedIds}
                              setCollapsedIds={setCollapsedIds}
                            />
                          ) : (
                            <div className="tree-empty muted" style={{ padding: '20px' }}>Стратегічні цілі відсутні у цьому файлі.</div>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                </section>
              )}
            </div>
          </div>
        )}
      </Container>
    </main>
  )
}

// ── Interactive Goals Tree Editor ────────────────────────────────────────────

function EditableGoalsTree({
  strategy,

  onDelete,
  onMove,
  onInsert,
  onAddStrategic,
  onAddOperational,
  onAddTask,
  editingId,
  editFields,
  setEditFields,
  startEdit,
  cancelEdit,
  saveEdit,
  collapsedIds,
  setCollapsedIds
}: EditableGoalsTreeProps) {
  const toggleCollapse = (id: string) => {
    setCollapsedIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const isCollapsed = (id: string) => collapsedIds.has(id)
  const strategicGoals = strategy.strategicGoals || []

  return (
    <div className="editable-goals-tree">
      {strategicGoals.map((goal, gIndex) => {
        const goalCollapsed = isCollapsed(goal.id)
        const isEditingGoal = editingId === goal.id

        return (
          <article key={goal.id} id={`goal-${goal.id}`} className="tree-node tree-node--strategic">
            <div className="tree-node__header tree-node__header--strategic">
              <button
                type="button"
                className="edit-tree__collapse-btn"
                onClick={(e) => {
                  e.stopPropagation()
                  toggleCollapse(goal.id)
                }}
                aria-expanded={!goalCollapsed}
              >
                <span className={`edit-tree__toggle ${goalCollapsed ? 'edit-tree__toggle--collapsed' : ''}`} aria-hidden="true">&gt;</span>
              </button>

              {isEditingGoal ? (
                <div className="edit-tree__editor-fields edit-tree__editor-fields--inline" onClick={(e) => e.stopPropagation()}>
                  <input
                    type="text"
                    className="edit-tree__input edit-tree__input--label"
                    value={editFields.label}
                    onChange={(e) => setEditFields(prev => ({ ...prev, label: e.target.value }))}
                    placeholder="№"
                  />
                  <input
                    type="text"
                    className="edit-tree__input edit-tree__input--title"
                    value={editFields.title}
                    onChange={(e) => setEditFields(prev => ({ ...prev, title: e.target.value }))}
                    placeholder="Назва стратегічної цілі"
                    autoFocus
                  />
                </div>
              ) : (
                <div className="edit-tree__text-content edit-tree__text-content--strategic">
                  <span className="edit-tree__title-prefix">
                    Стратегічна ціль {goal.label ? `${goal.label}.` : ''}
                  </span>
                  <span className="node-title node-title--strategic">
                    {goal.title || 'Без назви'}
                  </span>
                </div>
              )}

              <div className="edit-tree__node-actions" onClick={(e) => e.stopPropagation()}>
                {isEditingGoal ? (
                  <>
                    <button type="button" className="edit-tree__action-btn edit-tree__action-btn--save" onClick={() => saveEdit(goal.id)} title="Зберегти">
                      <CheckIcon />
                    </button>
                    <button type="button" className="edit-tree__action-btn edit-tree__action-btn--cancel" onClick={cancelEdit} title="Скасувати">
                      <CancelIcon />
                    </button>
                  </>
                ) : (
                  <>
                    <button type="button" className="edit-tree__action-btn" onClick={() => startEdit(goal)} title="Редагувати">
                      <EditIcon />
                    </button>
                    <button type="button" className="edit-tree__action-btn" onClick={() => onMove(goal.id, 'up')} disabled={gIndex === 0} title="Перемістити вгору">
                      <MoveUpIcon />
                    </button>
                    <button type="button" className="edit-tree__action-btn" onClick={() => onMove(goal.id, 'down')} disabled={gIndex === strategicGoals.length - 1} title="Перемістити вниз">
                      <MoveDownIcon />
                    </button>
                    <button type="button" className="edit-tree__action-btn edit-tree__action-btn--danger" onClick={() => { if(confirm('Ви впевнені, що хочете видалити цю стратегічну ціль та всі її оперативні цілі й завдання?')) onDelete(goal.id) }} title="Видалити">
                      <DeleteIcon />
                    </button>
                  </>
                )}
              </div>
            </div>

            {!goalCollapsed && (
              <div className="tree-node__children tree-node__children--strategic">
                <p className="edit-tree__branch-label">Оперативні цілі</p>

                {(goal.operationalGoals || []).map((op, opIndex) => {
                  const opCollapsed = isCollapsed(op.id)
                  const isEditingOp = editingId === op.id

                  return (
                    <section key={op.id} id={`goal-${op.id}`} className="tree-node tree-node--operational">
                      <div className="tree-node__header tree-node__header--operational">
                        <button
                          type="button"
                          className="edit-tree__collapse-btn"
                          onClick={(e) => {
                            e.stopPropagation()
                            toggleCollapse(op.id)
                          }}
                          aria-expanded={!opCollapsed}
                        >
                          <span className={`edit-tree__toggle ${opCollapsed ? 'edit-tree__toggle--collapsed' : ''}`} aria-hidden="true">&gt;</span>
                        </button>

                        {isEditingOp ? (
                          <div className="edit-tree__editor-fields edit-tree__editor-fields--inline" onClick={(e) => e.stopPropagation()}>
                            <input
                              type="text"
                              className="edit-tree__input edit-tree__input--label"
                              value={editFields.label}
                              onChange={(e) => setEditFields(prev => ({ ...prev, label: e.target.value }))}
                              placeholder="№"
                            />
                            <input
                              type="text"
                              className="edit-tree__input edit-tree__input--title"
                              value={editFields.title}
                              onChange={(e) => setEditFields(prev => ({ ...prev, title: e.target.value }))}
                              placeholder="Назва оперативної цілі"
                              autoFocus
                            />
                          </div>
                        ) : (
                          <div className="edit-tree__text-content edit-tree__text-content--operational">
                            <span className="edit-tree__label-num">
                              {op.label ? `${op.label}.` : ''}
                            </span>
                            <span className="node-title node-title--operational">
                              {op.title || 'Без назви'}
                            </span>
                          </div>
                        )}

                        <div className="edit-tree__node-actions" onClick={(e) => e.stopPropagation()}>
                          {isEditingOp ? (
                            <>
                              <button type="button" className="edit-tree__action-btn edit-tree__action-btn--save" onClick={() => saveEdit(op.id)} title="Зберегти">
                                <CheckIcon />
                              </button>
                              <button type="button" className="edit-tree__action-btn edit-tree__action-btn--cancel" onClick={cancelEdit} title="Скасувати">
                                <CancelIcon />
                              </button>
                            </>
                          ) : (
                            <>
                              <button type="button" className="edit-tree__action-btn" onClick={() => startEdit(op)} title="Редагувати">
                                <EditIcon />
                              </button>
                              <button type="button" className="edit-tree__action-btn" onClick={() => onMove(op.id, 'up')} disabled={opIndex === 0} title="Перемістити вгору">
                                <MoveUpIcon />
                              </button>
                              <button type="button" className="edit-tree__action-btn" onClick={() => onMove(op.id, 'down')} disabled={opIndex === goal.operationalGoals.length - 1} title="Перемістити вниз">
                                <MoveDownIcon />
                              </button>
                              <button type="button" className="edit-tree__action-btn edit-tree__action-btn--danger" onClick={() => { if(confirm('Ви впевнені, що хочете видалити цю оперативну ціль та всі її завдання?')) onDelete(op.id) }} title="Видалити">
                                <DeleteIcon />
                              </button>
                            </>
                          )}
                        </div>
                      </div>

                      {!opCollapsed && (
                        <div className="tree-node__children tree-node__children--operational">
                          {(op.programTasks || []).map((task, tIndex) => {
                            const isEditingTask = editingId === task.id

                            return (
                              <div key={task.id} className="edit-tree__task-wrap">
                                <div className="tree-node--leaf tree-node--leaf--task">
                                  <span className="edit-tree__task-marker" aria-hidden="true"></span>

                                  {isEditingTask ? (
                                    <div className="edit-tree__editor-fields edit-tree__editor-fields--task">
                                      <input
                                        type="text"
                                        className="edit-tree__input edit-tree__input--label"
                                        value={editFields.label}
                                        onChange={(e) => setEditFields(prev => ({ ...prev, label: e.target.value }))}
                                        placeholder="№"
                                      />
                                      <textarea
                                        className="edit-tree__textarea"
                                        value={editFields.description}
                                        onChange={(e) => setEditFields(prev => ({ ...prev, description: e.target.value }))}
                                        placeholder="Опис завдання"
                                        rows={2}
                                        autoFocus
                                      />
                                    </div>
                                  ) : (
                                    <div className="edit-tree__text-content edit-tree__text-content--task">
                                      <strong className="edit-tree__task-label">
                                        {task.label ? `${task.label}. ` : ''}
                                      </strong>
                                      <span className="node-desc">
                                        {task.description || 'Без опису'}
                                      </span>
                                    </div>
                                  )}

                                  <div className="edit-tree__node-actions edit-tree__node-actions--task">
                                    {isEditingTask ? (
                                      <>
                                        <button type="button" className="edit-tree__action-btn edit-tree__action-btn--save" onClick={() => saveEdit(task.id)} title="Зберегти">
                                          <CheckIcon />
                                        </button>
                                        <button type="button" className="edit-tree__action-btn edit-tree__action-btn--cancel" onClick={cancelEdit} title="Скасувати">
                                          <CancelIcon />
                                        </button>
                                      </>
                                    ) : (
                                      <>
                                        <button type="button" className="edit-tree__action-btn" onClick={() => startEdit(task)} title="Редагувати">
                                          <EditIcon />
                                        </button>
                                        <button type="button" className="edit-tree__action-btn" onClick={() => onMove(task.id, 'up')} disabled={tIndex === 0} title="Перемістити вгору">
                                          <MoveUpIcon />
                                        </button>
                                        <button type="button" className="edit-tree__action-btn" onClick={() => onMove(task.id, 'down')} disabled={tIndex === op.programTasks.length - 1} title="Перемістити вниз">
                                          <MoveDownIcon />
                                        </button>
                                        <button type="button" className="edit-tree__action-btn edit-tree__action-btn--danger" onClick={() => onDelete(task.id)} title="Видалити">
                                          <DeleteIcon />
                                        </button>
                                      </>
                                    )}
                                  </div>
                                </div>

                                {tIndex < op.programTasks.length - 1 && (
                                  <div
                                    className="tree-insert-line tree-insert-line--task"
                                    onClick={() => onInsert(task.id, 'task')}
                                    title="Вставити завдання сюди"
                                  >
                                    <div className="tree-insert-line__bar"></div>
                                    <span className="tree-insert-line__btn">+</span>
                                  </div>
                                )}
                              </div>
                            )
                          })}

                          <button
                            type="button"
                            className="edit-tree__append-btn"
                            onClick={() => onAddTask(op.id)}
                          >
                            + Додати завдання
                          </button>
                        </div>
                      )}

                      {opIndex < goal.operationalGoals.length - 1 && (
                        <div
                          className="tree-insert-line tree-insert-line--operational"
                          onClick={() => onInsert(op.id, 'operational')}
                          title="Вставити оперативну ціль сюди"
                        >
                          <div className="tree-insert-line__bar"></div>
                          <span className="tree-insert-line__btn">+</span>
                        </div>
                      )}
                    </section>
                  )
                })}

                <button
                  type="button"
                  className="edit-tree__append-btn edit-tree__append-btn--op"
                  onClick={() => onAddOperational(goal.id)}
                >
                  + Додати оперативну ціль
                </button>
              </div>
            )}

            {gIndex < strategicGoals.length - 1 && (
              <div
                className="tree-insert-line tree-insert-line--strategic"
                onClick={() => onInsert(goal.id, 'strategic')}
                title="Вставити стратегічну ціль сюди"
              >
                <div className="tree-insert-line__bar"></div>
                <span className="tree-insert-line__btn">+</span>
              </div>
            )}
          </article>
        )
      })}

      <button
        type="button"
        className="edit-tree__append-btn edit-tree__append-btn--strategic"
        onClick={onAddStrategic}
      >
        + Додати стратегічну ціль
      </button>
    </div>
  )
}
