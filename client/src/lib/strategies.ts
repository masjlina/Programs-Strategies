import { apiGet } from "./api";
import {
  IS_DEMO_MODE,
  getMockCatalogEntry,
  getMockStrategyResponse,
} from "./mockData";

interface Unit {
  id: string;
  name: string;
  type: "Region" | "District" | "Community";
}

export interface CatalogEntry {
  id: string;
  city: string;
  unitId: string;
  strategyId: string;
  title: string;
  period: string;
  summary: string;
  directions: string[];
  status: "active" | "archive";
  strategyUrl: string | null;
  officialSourceUrl: string | null;
  fileUrl: string | null;
}

interface StrategyTask {
  id?: string;
  label?: string;
  number?: string | number;
  description?: string;
}

interface OperationalGoal {
  id?: string;
  label?: string;
  number?: string | number;
  title?: string;
  programTasks?: StrategyTask[];
}

interface StrategicGoal {
  id?: string;
  label?: string;
  number?: string | number;
  title?: string;
  operationalGoals?: OperationalGoal[];
}

interface Strategy {
  id: string;
  title?: string;
  strategyUrl?: string | null;
  regionId?: string | null;
  districtId?: string | null;
  communityId?: string | null;
  strategicGoals?: StrategicGoal[];
}

export interface StrategyResponse {
  unit?: Unit;
  strategy: Strategy;
}

let strategiesCache: Promise<CatalogEntry[]> | null = null;
let unitsCache: Promise<Map<string, Unit>> | null = null;
const strategyDetailsCache = new Map<string, Promise<Strategy>>();

function normalize(value: unknown): string {
  return String(value ?? "")
    .trim()
    .toLowerCase();
}

function extractPeriod(title?: string): string {
  const match = title?.match(/(20\d{2})\s*[–-]\s*(20\d{2})/);
  if (!match) return "";
  return `${match[1]}–${match[2]}`;
}

function getStatus(period: string): "active" | "archive" {
  const endYear = Number(period?.match(/20\d{2}\s*[–-]\s*(20\d{2})/)?.[1]);
  if (!endYear) return "active";
  return endYear < new Date().getFullYear() ? "archive" : "active";
}

function getCityFromUnitName(name?: string): string {
  return (
    name
      ?.replace(/\s+міська територіальна громада$/i, "")
      ?.replace(/\s+територіальна громада$/i, "")
      ?.trim() || "Невідома територія"
  );
}

function normalizeTask(
  task: StrategyTask,
): Required<Pick<StrategyTask, "label" | "description">> & StrategyTask {
  const label = task.label ?? String(task.number ?? "");
  return {
    id: (task as any).id ?? label,
    ...task,
    label,
    description: task.description ?? "",
  };
}

function normalizeOperationalGoal(goal: OperationalGoal): OperationalGoal {
  const programTasks = goal.programTasks ?? [];
  const label = goal.label ?? String(goal.number ?? "");
  return {
    id: (goal as any).id ?? label,
    ...goal,
    label,
    title: goal.title ?? "",
    programTasks: programTasks.map(normalizeTask),
  };
}

function normalizeStrategicGoal(goal: StrategicGoal): StrategicGoal {
  const operationalGoals = goal.operationalGoals ?? [];
  const label = goal.label ?? String(goal.number ?? "");
  return {
    id: (goal as any).id ?? label,
    ...goal,
    label,
    title: goal.title ?? "",
    operationalGoals: operationalGoals.map(normalizeOperationalGoal),
  };
}

export function normalizeStrategy(strategy: Strategy): Strategy {
  const strategicGoals = strategy.strategicGoals ?? [];
  return {
    ...strategy,
    title: strategy.title ?? "",
    strategyUrl: strategy.strategyUrl ?? null,
    regionId: strategy.regionId ?? null,
    districtId: strategy.districtId ?? null,
    communityId: strategy.communityId ?? null,
    strategicGoals: strategicGoals.map(normalizeStrategicGoal),
  };
}

function getStrategyUnitId(strategy: Strategy): string | null {
  return (
    strategy.communityId ?? strategy.districtId ?? strategy.regionId ?? null
  );
}

function getDirections(strategy: Strategy): string[] {
  const directions = (strategy.strategicGoals ?? [])
    .flatMap((strategicGoal) => strategicGoal.operationalGoals ?? [])
    .map((operationalGoal) => operationalGoal.title)
    .filter((item): item is string => Boolean(item));
  return [...new Set(directions)];
}

function buildCatalogEntry(strategy: Strategy, unit?: Unit): CatalogEntry {
  const normalizedStrategy = normalizeStrategy(strategy);
  const period = extractPeriod(normalizedStrategy.title);
  const directions = getDirections(normalizedStrategy);
  const sourceUrl = normalizedStrategy.strategyUrl || null;
  return {
    id: normalizedStrategy.id,
    city: getCityFromUnitName(unit?.name),
    unitId: getStrategyUnitId(normalizedStrategy) ?? "",
    strategyId: normalizedStrategy.id,
    title: normalizedStrategy.title ?? "",
    period,
    summary: unit?.name
      ? `Стратегічний документ: ${unit.name}.`
      : "Стратегічний документ територіальної одиниці.",
    directions,
    status: getStatus(period),
    strategyUrl: sourceUrl,
    officialSourceUrl: sourceUrl,
    fileUrl: sourceUrl?.toLowerCase().endsWith(".pdf") ? sourceUrl : null,
  };
}

async function getUnitsMap(): Promise<Map<string, Unit>> {
  if (!unitsCache) {
    unitsCache = Promise.all([
      apiGet<any[]>("/api/Regions"),
      apiGet<any[]>("/api/Districts"),
      apiGet<any[]>("/api/Communities"),
    ]).then(([regions, districts, communities]) => {
      const allUnits: Unit[] = [];
      regions.forEach((r) =>
        allUnits.push({
          id: String(r.id),
          name: r.nameFull || r.name,
          type: "Region",
        }),
      );
      districts.forEach((d) =>
        allUnits.push({
          id: String(d.id),
          name: d.nameFull || d.name,
          type: "District",
        }),
      );
      communities.forEach((c) =>
        allUnits.push({
          id: String(c.id),
          name: c.nameFull || c.name,
          type: "Community",
        }),
      );
      return new Map(allUnits.map((unit) => [unit.id, unit]));
    });
  }
  return unitsCache;
}

function getStrategyById(id: string): Promise<Strategy> {
  if (!strategyDetailsCache.has(id)) {
    strategyDetailsCache.set(
      id,
      apiGet<Strategy>(`/api/Strategies/${id}`).then(normalizeStrategy),
    );
  }
  return strategyDetailsCache.get(id)!;
}

async function getCatalog(): Promise<CatalogEntry[]> {
  if (!strategiesCache) {
    strategiesCache = Promise.all([
      apiGet<Strategy[]>("/api/Strategies"),
      getUnitsMap(),
    ]).then(([strategies, unitsById]) =>
      strategies.map((strategy) => {
        const unitId = getStrategyUnitId(strategy);
        const unit = unitId ? unitsById.get(unitId) : undefined;
        return buildCatalogEntry(strategy, unit);
      }),
    );
  }
  return strategiesCache;
}

export async function getCities(): Promise<string[]> {
  const catalog = await getCatalog();
  const cities = [...new Set(catalog.map((item) => item.city))];
  return cities.sort((a, b) => a.localeCompare(b, "uk"));
}

export async function getDirectionsList(): Promise<string[]> {
  const catalog = await getCatalog();
  const all = catalog.flatMap((item) => item.directions ?? []);
  return [...new Set(all)].sort((a, b) => a.localeCompare(b, "uk"));
}

export { getDirectionsList as getDirections };

export async function searchStrategies(query: string): Promise<CatalogEntry[]> {
  const q = normalize(query);
  if (!q) return [];
  const catalog = await getCatalog();
  return catalog.filter((item) => {
    const haystack = [
      item.city,
      item.title,
      item.summary,
      item.period,
      ...(item.directions ?? []),
    ]
      .join(" ")
      .toLowerCase();
    return haystack.includes(q);
  });
}

export async function getCatalogEntryById(
  id: string,
): Promise<CatalogEntry | null> {
  if (IS_DEMO_MODE) return getMockCatalogEntry(id);

  const [strategy, unitsById] = await Promise.all([
    getStrategyById(id),
    getUnitsMap(),
  ]);
  const unitId = getStrategyUnitId(strategy);
  const unit = unitId ? unitsById.get(unitId) : undefined;
  if (!strategy) return null;
  return buildCatalogEntry(strategy, unit);
}

export async function getStrategiesByCity(
  city: string,
): Promise<CatalogEntry[]> {
  if (!city) return [];
  const catalog = await getCatalog();
  return catalog.filter((item) => item.city === city);
}

export async function loadStrategyForCatalogEntry(
  catalogEntry: CatalogEntry,
): Promise<StrategyResponse> {
  if (IS_DEMO_MODE) {
    const response = getMockStrategyResponse(
      catalogEntry.strategyId ?? catalogEntry.id,
    );
    if (response) return response;
  }

  const [strategy, unitsById] = await Promise.all([
    getStrategyById(catalogEntry.strategyId ?? catalogEntry.id),
    getUnitsMap(),
  ]);
  const unit = unitsById.get(catalogEntry.unitId);
  return { unit, strategy };
}
