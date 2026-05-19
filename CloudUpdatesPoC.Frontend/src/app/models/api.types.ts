export type Provider = 'AWS' | 'Azure';

export interface StatsResponse {
  indexed: number;
  providers: Partial<Record<Provider, number>> & Record<string, number>;
}

export interface HealthResponse {
  status: string;
  time: string;
}

export interface QueryRequest {
  services: string[];
  question: string;
}

export interface Source {
  title: string;
  provider: Provider | string;
  url: string;
  publishedAt: string;
  score: number;
}

export interface QueryResponse {
  answer: string;
  sources: Source[];
}

export type ApiErrorKind = 'rate-limit' | 'offline' | 'server' | 'unknown';

export interface ApiError {
  kind: ApiErrorKind;
  message: string;
  status?: number;
}
