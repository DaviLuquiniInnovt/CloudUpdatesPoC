import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ApiService } from './services/api.service';
import { ApiError, QueryRequest, QueryResponse, StatsResponse } from './models/api.types';
import { HeaderComponent } from './components/header/header.component';
import { QueryFormComponent } from './components/query-form/query-form.component';
import { AnswerCardComponent } from './components/answer-card/answer-card.component';
import { SourcesPanelComponent } from './components/sources-panel/sources-panel.component';

const HEALTH_POLL_INTERVAL_MS = 30_000;

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HeaderComponent, QueryFormComponent, AnswerCardComponent, SourcesPanelComponent],
  template: `
    <div class="min-h-screen bg-slate-950 text-slate-100 antialiased">
      <div class="bg-grid pointer-events-none fixed inset-0 -z-10 opacity-40"></div>
      <div
        class="pointer-events-none fixed inset-x-0 top-0 -z-10 h-[480px] bg-gradient-to-b from-indigo-500/10 via-indigo-500/5 to-transparent"
        aria-hidden="true"
      ></div>

      <app-header [stats]="stats()" [apiOnline]="apiOnline()" [loading]="statsLoading()" />

      <main class="mx-auto max-w-6xl px-6 py-10">
        <section class="mb-8 max-w-2xl">
          <h2
            class="bg-gradient-to-br from-white via-slate-100 to-slate-400 bg-clip-text text-3xl font-semibold tracking-tight text-transparent sm:text-4xl"
          >
            Quais updates impactam o seu workload?
          </h2>
          <p class="mt-3 text-sm leading-relaxed text-slate-400">
            Liste os serviços de AWS e Azure que você usa, faça uma pergunta e a PoC analisa os
            últimos updates publicados nas RSS oficiais para indicar o que importa pra você.
          </p>
        </section>

        @if (queryError(); as err) {
          <div
            class="mb-6 flex items-start gap-3 rounded-2xl border p-4 text-sm shadow-lg"
            [class]="errorBoxClasses()"
            role="alert"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
              class="mt-0.5 h-5 w-5 shrink-0"
              aria-hidden="true"
            >
              @if (err.kind === 'rate-limit') {
                <circle cx="12" cy="12" r="10" />
                <path d="M12 6v6l4 2" />
              } @else if (err.kind === 'offline') {
                <path d="M5 12.55a11 11 0 0 1 14 0" />
                <path d="M1.42 9a16 16 0 0 1 21.16 0" />
                <path d="M8.53 16.11a6 6 0 0 1 6.95 0" />
                <line x1="12" y1="20" x2="12.01" y2="20" />
              } @else {
                <circle cx="12" cy="12" r="10" />
                <path d="M12 8v4" />
                <path d="M12 16h.01" />
              }
            </svg>
            <div class="flex-1">
              <p class="font-semibold">{{ errorTitle() }}</p>
              <p class="mt-0.5 opacity-90">{{ err.message }}</p>
            </div>
            <button
              type="button"
              (click)="clearError()"
              class="rounded-md p-1 text-current opacity-60 transition-opacity hover:opacity-100 focus:outline-none focus-visible:ring-2 focus-visible:ring-current"
              aria-label="Fechar alerta"
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
                class="h-4 w-4"
              >
                <path d="M18 6 6 18M6 6l12 12" />
              </svg>
            </button>
          </div>
        }

        <div class="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
          <div class="space-y-6">
            <app-query-form
              [loading]="queryLoading()"
              [totalIndexed]="stats()?.indexed ?? 0"
              (submitQuery)="onSubmit($event)"
            />

            @if (queryLoading()) {
              <div
                class="rounded-2xl border border-slate-800/80 bg-slate-900/60 p-8 shadow-xl shadow-black/20"
              >
                <div class="flex items-center gap-3">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2.5"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    class="h-5 w-5 animate-spin text-indigo-400"
                    aria-hidden="true"
                  >
                    <path d="M21 12a9 9 0 1 1-6.219-8.56" />
                  </svg>
                  <p class="text-sm text-slate-300">
                    Analisando ~{{ stats()?.indexed ?? 300 }} updates com o RAG…
                  </p>
                </div>
                <div class="mt-5 space-y-2.5">
                  <div class="h-3 w-3/4 animate-pulse rounded bg-slate-800"></div>
                  <div class="h-3 w-full animate-pulse rounded bg-slate-800"></div>
                  <div class="h-3 w-5/6 animate-pulse rounded bg-slate-800"></div>
                  <div class="h-3 w-2/3 animate-pulse rounded bg-slate-800"></div>
                </div>
              </div>
            } @else if (response(); as r) {
              <app-answer-card [answer]="r.answer" (reset)="resetQuery()" />
            }
          </div>

          <div>
            @if (response(); as r) {
              <app-sources-panel [sources]="r.sources" />
            } @else if (!queryLoading()) {
              <aside
                class="rounded-2xl border border-dashed border-slate-800/70 bg-slate-900/30 p-6 text-sm text-slate-500"
              >
                <p class="font-medium text-slate-400">Sources</p>
                <p class="mt-2 leading-relaxed">
                  Os updates usados como contexto pelo RAG aparecerão aqui depois que você fizer
                  uma pergunta.
                </p>
              </aside>
            }
          </div>
        </div>

        <footer class="mt-16 border-t border-slate-800/70 pt-6 text-center text-xs text-slate-600">
          PoC RAG · Embeddings Gemini · Vector store local · API em
          <code class="rounded bg-slate-800/60 px-1.5 py-0.5 text-slate-400">localhost:5267</code>
        </footer>
      </main>
    </div>
  `,
})
export class App implements OnInit {
  private readonly api = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly stats = signal<StatsResponse | null>(null);
  protected readonly statsLoading = signal<boolean>(true);
  protected readonly apiOnline = signal<boolean>(false);

  protected readonly response = signal<QueryResponse | null>(null);
  protected readonly queryLoading = signal<boolean>(false);
  protected readonly queryError = signal<ApiError | null>(null);

  protected readonly errorTitle = computed(() => {
    const err = this.queryError();
    if (!err) return '';
    switch (err.kind) {
      case 'rate-limit':
        return 'Limite de uso atingido';
      case 'offline':
        return 'API offline';
      case 'server':
        return 'Erro no servidor';
      default:
        return 'Ops, algo deu errado';
    }
  });

  protected readonly errorBoxClasses = computed(() => {
    const kind = this.queryError()?.kind;
    if (kind === 'rate-limit') {
      return 'border-amber-500/30 bg-amber-500/10 text-amber-200';
    }
    if (kind === 'offline') {
      return 'border-slate-700 bg-slate-900/80 text-slate-300';
    }
    return 'border-rose-500/30 bg-rose-500/10 text-rose-200';
  });

  ngOnInit(): void {
    this.loadStats();
    this.pingHealth();
    const interval = setInterval(() => this.pingHealth(), HEALTH_POLL_INTERVAL_MS);
    this.destroyRef.onDestroy(() => clearInterval(interval));
  }

  protected onSubmit(payload: QueryRequest): void {
    this.queryLoading.set(true);
    this.queryError.set(null);
    this.response.set(null);

    this.api
      .query(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.response.set(res);
          this.queryLoading.set(false);
        },
        error: (err: ApiError) => {
          this.queryError.set(err);
          this.queryLoading.set(false);
          if (err.kind === 'offline') {
            this.apiOnline.set(false);
          }
        },
      });
  }

  protected resetQuery(): void {
    this.response.set(null);
    this.queryError.set(null);
  }

  protected clearError(): void {
    this.queryError.set(null);
  }

  private loadStats(): void {
    this.statsLoading.set(true);
    this.api
      .stats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (s) => {
          this.stats.set(s);
          this.statsLoading.set(false);
        },
        error: () => {
          this.statsLoading.set(false);
        },
      });
  }

  private pingHealth(): void {
    this.api
      .health()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.apiOnline.set(true),
        error: () => this.apiOnline.set(false),
      });
  }
}
