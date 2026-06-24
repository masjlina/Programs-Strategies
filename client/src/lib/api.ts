const DEFAULT_API_BASE_URL = "http://localhost:5257";

interface ApiError {
  message?: string;
  errors?: string[];
}

type UnitType = "Community" | "District" | "Region";

interface UploadItem {
  type: UnitType;
  regionId: string | number;
  districtId?: string | number;
  communityId?: string | number;
}

interface ReferenceData {
  regions: any[];
  districts: any[];
  communities: any[];
  strategies: any[];
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

// In-memory access token storage
let accessToken: string | null = null;
let isRefreshing = false;
let refreshSubscribers: ((token: string) => void)[] = [];

export function setAccessToken(token: string | null) {
  accessToken = token;
}

export function getAccessToken(): string | null {
  return accessToken;
}

function subscribeTokenRefresh(cb: (token: string) => void) {
  refreshSubscribers.push(cb);
}

function onRefreshed(token: string) {
  refreshSubscribers.forEach((cb) => cb(token));
  refreshSubscribers = [];
}

export async function apiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    ...options,
    headers,
    credentials: "include",
  });

  // Handle 401 Unauthorized (unless it's sign-in or a refresh request itself)
  if (response.status === 401 && path !== "/api/sign-in" && path !== "/api/refresh") {
    if (!isRefreshing) {
      isRefreshing = true;
      try {
        const refreshResponse = await fetch(`${getApiBaseUrl()}/api/refresh`, {
          method: "POST",
          credentials: "include",
        });

        if (refreshResponse.ok) {
          const data = (await refreshResponse.json()) as { accessToken: string };
          accessToken = data.accessToken;
          onRefreshed(data.accessToken);
          isRefreshing = false;
        } else {
          isRefreshing = false;
          accessToken = null;
          throw new Error("Session expired");
        }
      } catch (err) {
        isRefreshing = false;
        accessToken = null;
        throw err;
      }
    }

    // Wait for the token refresh to complete and retry the request
    return new Promise<T>((resolve, reject) => {
      subscribeTokenRefresh(async (newToken) => {
        try {
          headers.set("Authorization", `Bearer ${newToken}`);
          const retryResponse = await fetch(`${getApiBaseUrl()}${path}`, {
            ...options,
            headers,
            credentials: "include",
          });

          if (!retryResponse.ok) {
            const payload = (await retryResponse.json().catch(() => null)) as ApiError | null;
            reject(new Error(payload?.message ?? payload?.errors?.[0] ?? `API request failed: ${retryResponse.status}`));
          } else {
            if (retryResponse.status === 204) {
              resolve({} as T);
            } else {
              resolve(retryResponse.json() as Promise<T>);
            }
          }
        } catch (e) {
          reject(e);
        }
      });
    });
  }

  if (!response.ok) {
    const payload = (await response.json().catch(() => null)) as ApiError | null;
    throw new Error(
      payload?.message ?? payload?.errors?.[0] ?? `API request failed: ${response.status}`,
    );
  }

  if (response.status === 204) {
    return {} as T;
  }

  return response.json() as Promise<T>;
}

export async function apiGet<T>(path: string): Promise<T> {
  return apiRequest<T>(path, { method: "GET" });
}

export async function apiPost<TResponse, TBody>(path: string, body: TBody): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
}

export async function apiPut<TResponse, TBody>(path: string, body: TBody): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
}

export async function apiPatch<TResponse, TBody>(path: string, body: TBody): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
}

export async function apiDelete<TResponse = void>(path: string): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: "DELETE",
  });
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
    if (item.districtId !== undefined) params.set("districtId", String(item.districtId));
  }

  if (item.type === "Community" && item.communityId !== undefined) {
    params.set("communityId", String(item.communityId));
  }

  return `/upload?${params.toString()}`;
}

export async function apiUploadDocument<TResponse>(file: File): Promise<TResponse> {
  const formData = new FormData();
  formData.append("file", file);

  return apiRequest<TResponse>("/api/UploadFile/parse-document", {
    method: "POST",
    body: formData,
  });
}

export interface SearchStrategyItem {
  id: string;
  title: string;
  regionId?: string | null;
  districtId?: string | null;
  communityId?: string | null;
}

export interface SearchItem {
  id: string;
  name: string;
  type: "Region" | "District" | "Community";
  regionId?: string | null;
  districtId?: string | null;
  communityId?: string | null;
  regionName: string;
  districtName: string;
  strategies: SearchStrategyItem[];
}

export interface SearchResult {
  items: SearchItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export async function searchUnits(
  query: string,
  filter: string,
  sort: string,
  page: number,
  pageSize: number,
  regionId?: string,
  districtId?: string
): Promise<SearchResult> {
  const params = new URLSearchParams({
    query,
    filter,
    sort,
    page: String(page),
    pageSize: String(pageSize),
  });
  if (regionId) params.append("regionId", regionId);
  if (districtId) params.append("districtId", districtId);
  return apiGet<SearchResult>(`/api/Search?${params.toString()}`);
}
