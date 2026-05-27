import type {IStrategicGoal} from "./strategicGoal.ts";

export interface IStrategy {
    id?: string;
    administrativeUnitId?: string,
    title: string
    strategicGoals?: IStrategicGoal[]
}