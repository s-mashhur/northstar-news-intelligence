import type { Dashboard, Monitor, SearchResult, TokenPair } from "./types";

const base = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5080/api";
const ACCESS = "newsintel_access";
const REFRESH = "newsintel_refresh";
const USER = "newsintel_user";

export const authStore = {
  access: () => typeof window === "undefined" ? null : localStorage.getItem(ACCESS),
  user: () => {
    if (typeof window === "undefined") return null;
    const raw = localStorage.getItem(USER);
    return raw ? JSON.parse(raw) : null;
  },
  save: (pair: TokenPair) => {
    localStorage.setItem(ACCESS, pair.accessToken);
    localStorage.setItem(REFRESH, pair.refreshToken);
    localStorage.setItem(USER, JSON.stringify(pair.user));
  },
  clear: () => {
    localStorage.removeItem(ACCESS); localStorage.removeItem(REFRESH); localStorage.removeItem(USER);
  }
};

async function request<T>(path: string, init: RequestInit = {}, retry = true): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Content-Type", "application/json");
  const token = authStore.access();
  if (token) headers.set("Authorization", `Bearer ${token}`);
  const response = await fetch(`${base}${path}`, { ...init, headers, cache: "no-store" });
  if (response.status === 401 && retry && typeof window !== "undefined") {
    const refreshToken = localStorage.getItem(REFRESH);
    if (refreshToken) {
      const refreshed = await fetch(`${base}/auth/refresh`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ refreshToken }) });
      if (refreshed.ok) {
        authStore.save(await refreshed.json());
        return request<T>(path, init, false);
      }
    }
    authStore.clear();
    window.location.href = "/login";
    throw new Error("Session expired");
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(problem?.title || problem?.detail || `Request failed (${response.status})`);
  }
  if (response.status === 204) return undefined as T;
  return response.json();
}

export const api = {
  login: (email: string, password: string) => request<TokenPair>("/auth/login", { method: "POST", body: JSON.stringify({ email, password }) }),
  register: (email: string, password: string) => request<TokenPair>("/auth/register", { method: "POST", body: JSON.stringify({ email, password }) }),
  dashboard: () => request<Dashboard>("/dashboard"),
  search: (query: string, days: number, language: string, size = 20) => request<SearchResult>("/news/search", { method: "POST", body: JSON.stringify({ query, days, language, size }) }),
  monitors: () => request<Monitor[]>("/monitors"),
  createMonitor: (payload: { name: string; query: string; language: string; days: number; intervalMinutes: number }) => request<Monitor>("/monitors", { method: "POST", body: JSON.stringify(payload) }),
  runMonitor: (id: string) => request<Monitor>(`/monitors/${id}/run`, { method: "POST" }),
  deleteMonitor: (id: string) => request<void>(`/monitors/${id}`, { method: "DELETE" }),
};
