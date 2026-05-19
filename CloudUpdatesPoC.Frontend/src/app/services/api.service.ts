import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';

import {
  ApiError,
  HealthResponse,
  QueryRequest,
  QueryResponse,
  StatsResponse,
} from '../models/api.types';

const API_BASE_URL = 'http://localhost:5267';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  stats(): Observable<StatsResponse> {
    return this.http
      .get<StatsResponse>(`${API_BASE_URL}/stats`)
      .pipe(catchError((err: HttpErrorResponse) => throwError(() => toApiError(err))));
  }

  health(): Observable<HealthResponse> {
    return this.http
      .get<HealthResponse>(`${API_BASE_URL}/health`)
      .pipe(catchError((err: HttpErrorResponse) => throwError(() => toApiError(err))));
  }

  query(payload: QueryRequest): Observable<QueryResponse> {
    return this.http
      .post<QueryResponse>(`${API_BASE_URL}/query`, payload)
      .pipe(catchError((err: HttpErrorResponse) => throwError(() => toApiError(err))));
  }
}

function toApiError(err: HttpErrorResponse): ApiError {
  if (err.status === 0) {
    return {
      kind: 'offline',
      message:
        'Não foi possível conectar ao backend. Suba o servidor em http://localhost:5267 e confirme que o CORS está habilitado.',
      status: 0,
    };
  }

  if (err.status === 429) {
    return {
      kind: 'rate-limit',
      message: 'Limite de uso do Gemini atingido. Tente novamente em alguns minutos.',
      status: 429,
    };
  }

  if (err.status >= 500) {
    return {
      kind: 'server',
      message: `Erro interno do servidor (${err.status}). Confira os logs do backend.`,
      status: err.status,
    };
  }

  return {
    kind: 'unknown',
    message: err.error?.message ?? err.message ?? 'Erro desconhecido ao chamar a API.',
    status: err.status,
  };
}
