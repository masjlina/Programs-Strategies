import type {AdministrativeUnitType} from "./administrativeUnitType.ts";
import type {IStrategy} from "./strategy.ts";

export interface IAdministrativeUnit {
    id?: string;
    name: string;
    type: AdministrativeUnitType
    strategies?: IStrategy[]
}