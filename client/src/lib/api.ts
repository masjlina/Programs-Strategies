const DEFAULT_API_BASE_URL = "http://localhost:5257";

interface ApiError {
  message?: string;
}

type UnitType = "Community" | "District" | "Region";

interface UploadItem {
  type: UnitType;
  regionId: string | number;
  districtId?: string | number;
  communityId?: string | number;
}

interface ReferenceData {
  regions: unknown[];
  districts: unknown[];
  communities: unknown[];
  strategies: unknown[];
}

const UNIT_TYPE_LABELS: Record<UnitType, string> = {
  Community: "Громада",
  District: "Район",
  Region: "Область",
};

export function getApiBaseUrl(): string {
  const configured = import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, "");
  return configured || DEFAULT_API_BASE_URL;
}

export async function apiGet<T>(path: string): Promise<T> {
  const response = await fetch(`${getApiBaseUrl()}${path}`);

  if (!response.ok) {
    const payload = (await response
      .json()
      .catch(() => null)) as ApiError | null;
    throw new Error(
      payload?.message ?? `API request failed: ${response.status}`,
    );
  }

  return response.json() as Promise<T>;
}

export async function apiPost<TResponse, TBody>(
  path: string,
  body: TBody,
): Promise<TResponse> {
  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    const payload = (await response
      .json()
      .catch(() => null)) as ApiError | null;
    throw new Error(
      payload?.message ?? `API request failed: ${response.status}`,
    );
  }

  return response.json() as Promise<TResponse>;
}

export async function fetchReferenceData(): Promise<ReferenceData> {
  const [regions, districts, communities, strategies] = await Promise.all([
    apiGet<unknown[]>("/api/Regions"),
    apiGet<unknown[]>("/api/Districts"),
    apiGet<unknown[]>("/api/Communities"),
    apiGet<unknown[]>("/api/Strategies"),
  ]);

  return { regions, districts, communities, strategies };
}

export function getUnitTypeLabel(type: UnitType | string): string {
  return UNIT_TYPE_LABELS[type as UnitType] ?? type;
}

export function buildUploadLink(item: UploadItem): string {
  const params = new URLSearchParams({
    type: item.type,
    regionId: String(item.regionId),
  });

  if (item.type === "District" || item.type === "Community") {
    if (item.districtId !== undefined)
      params.set("districtId", String(item.districtId));
  }

  if (item.type === "Community" && item.communityId !== undefined) {
    params.set("communityId", String(item.communityId));
  }

  return `/upload?${params.toString()}`;
}
