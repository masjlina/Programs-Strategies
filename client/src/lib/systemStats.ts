import { apiGet } from "./api";

export interface DailyStrategyCount {
  date: string;
  count: number;
}

export interface SystemStats {
  regionsCount: number;
  communitiesCount: number;
  totalStrategiesCount: number;
  communitiesWithoutStrategiesCount: number;
  averageStrategiesPerCommunity: number;
  communitiesWithWebsiteCount: number;
  communitiesWithWebsitePercent: number;
  strategiesLastMonthByDay: DailyStrategyCount[];
}

export function fetchSystemStats(): Promise<SystemStats> {
  return apiGet<SystemStats>("/api/Stats");
}
