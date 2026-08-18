export type AuthUser = { id: string; email: string; role: string };
export type TokenPair = { accessToken: string; refreshToken: string; expiresAt: string; user: AuthUser };
export type Article = {
  id: string; title: string; description: string; url: string; imageUrl?: string | null;
  source: string; language?: string | null; publishedAt: string; sentiment: string; sentimentScore?: number | null;
};
export type SearchResult = { items: Article[]; totalResults: number; next?: string | null; provider: string };
export type Metric = { label: string; value: string; delta: string; tone: string };
export type Point = { label: string; value: number };
export type SourceStat = { name: string; count: number; percentage: number };
export type Dashboard = {
  metrics: Metric[]; volume: Point[]; sources: SourceStat[];
  sentiment: { positive: number; neutral: number; negative: number };
  recentArticles: Article[]; provider: string;
};
export type Monitor = {
  id: string; name: string; query: string; language?: string | null; days: number;
  intervalMinutes: number; isActive: boolean; lastRunAt?: string | null; articleCount: number; createdAt: string;
};
