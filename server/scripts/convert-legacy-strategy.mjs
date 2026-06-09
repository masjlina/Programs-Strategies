import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

const LEGACY_PATH =
  process.argv[2] ??
  'D:/Учоба/BC/mvp/Programs-Strategies/client/src/data/administrative_units/if-mtg-001.json'

const IVANO_FRANKIVSK_MTG = {
  communityId: '0efb1476-3e92-4753-a74d-cdb14d7a7b99',
  kattotgId: 'UA26040190000081578',
  strategyUrl: 'https://mrada.if.ua/',
}

function convertTask(task) {
  return {
    label: task.label,
    number: task.number,
    description: task.description,
  }
}

function convertOperationalGoal(goal) {
  const tasks = goal.tasks ?? goal.programTasks ?? []

  return {
    label: goal.label,
    number: goal.number,
    title: goal.title,
    programTasks: tasks.map(convertTask),
  }
}

function convertStrategicGoal(goal) {
  const operationalGoals = goal.operational_goals ?? goal.operationalGoals ?? []

  return {
    label: goal.label,
    number: goal.number,
    title: goal.title,
    operationalGoals: operationalGoals.map(convertOperationalGoal),
  }
}

function convertLegacyFile(filePath) {
  const raw = JSON.parse(fs.readFileSync(filePath, 'utf8'))
  const unit = raw.administrative_unit ?? raw
  const strategies = unit.strategies ?? raw.strategies ?? [raw]
  const strategy = strategies[0]

  if (!strategy?.title) {
    throw new Error('Legacy file does not contain a strategy with title')
  }

  const strategicGoals = strategy.strategic_goals ?? strategy.strategicGoals ?? []

  return {
    title: strategy.title,
    strategyUrl: strategy.strategyUrl ?? strategy.strategy_url ?? IVANO_FRANKIVSK_MTG.strategyUrl,
    regionId: null,
    districtId: null,
    communityId: IVANO_FRANKIVSK_MTG.communityId,
    strategicGoals: strategicGoals.map(convertStrategicGoal),
  }
}

const payload = convertLegacyFile(LEGACY_PATH)
const outputPath = path.join(__dirname, '../imports/ivano-frankivsk-mtg-strategy.json')

fs.mkdirSync(path.dirname(outputPath), { recursive: true })
fs.writeFileSync(outputPath, JSON.stringify(payload, null, 2), 'utf8')

console.log(JSON.stringify({
  outputPath,
  communityId: payload.communityId,
  kattotgId: IVANO_FRANKIVSK_MTG.kattotgId,
  title: payload.title,
  strategicGoals: payload.strategicGoals.length,
  programTasks: payload.strategicGoals.reduce(
    (sum, sg) =>
      sum + sg.operationalGoals.reduce((inner, og) => inner + og.programTasks.length, 0),
    0,
  ),
}))
