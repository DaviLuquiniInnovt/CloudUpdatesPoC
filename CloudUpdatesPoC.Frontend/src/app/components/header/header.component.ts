import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { StatsResponse } from '../../models/api.types';

@Component({
  selector: 'app-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header
      class="sticky top-0 z-40 border-b border-slate-800/80 bg-slate-950/85 backdrop-blur-md"
    >
      <div
        class="mx-auto flex max-w-6xl flex-col gap-4 px-6 py-4 sm:flex-row sm:items-center sm:justify-between"
      >
        <div class="flex items-center gap-3">
          <div
            class="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-indigo-500 to-cyan-500 shadow-lg shadow-indigo-500/30"
            aria-hidden="true"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
              class="h-5 w-5 text-white"
            >
              <path d="M17.5 19a4.5 4.5 0 1 0-1.6-8.7 6 6 0 0 0-11.6 2.4A3.5 3.5 0 0 0 5 19h12.5Z" />
              <path d="m9 15 2 2 4-4" />
            </svg>
          </div>
          <div class="flex flex-col">
            <h1 class="text-base font-semibold tracking-tight text-white sm:text-lg">
              Cloud Updates Insights
            </h1>
            <p class="text-xs text-slate-400">RAG sobre updates da AWS e Azure</p>
          </div>
        </div>

        <div class="flex flex-wrap items-center gap-2">
          @if (loading()) {
            <span
              class="inline-flex h-7 w-28 animate-pulse rounded-full bg-slate-800/70"
              aria-label="Carregando estatísticas"
            ></span>
          } @else if (stats(); as s) {
            <span
              class="inline-flex items-center gap-1.5 rounded-full border border-slate-700/70 bg-slate-900 px-3 py-1 text-xs font-medium text-slate-300"
              title="Total de updates indexados"
            >
              <span class="text-slate-400">indexados</span>
              <span class="tabular-nums text-white">{{ s.indexed }}</span>
            </span>
            <span
              class="inline-flex items-center gap-1.5 rounded-full border border-amber-500/30 bg-amber-500/10 px-3 py-1 text-xs font-medium text-amber-300"
              title="Updates da AWS"
            >
              <span class="h-1.5 w-1.5 rounded-full bg-amber-400"></span>
              AWS
              <span class="tabular-nums text-amber-200">{{ awsCount() }}</span>
            </span>
            <span
              class="inline-flex items-center gap-1.5 rounded-full border border-sky-500/30 bg-sky-500/10 px-3 py-1 text-xs font-medium text-sky-300"
              title="Updates da Azure"
            >
              <span class="h-1.5 w-1.5 rounded-full bg-sky-400"></span>
              Azure
              <span class="tabular-nums text-sky-200">{{ azureCount() }}</span>
            </span>
          }

          <span
            class="inline-flex items-center gap-2 rounded-full border px-3 py-1 text-xs font-medium transition-colors"
            [class]="statusClasses()"
            [attr.aria-label]="apiOnline() ? 'API online' : 'API offline'"
          >
            <span class="relative flex h-2 w-2">
              @if (apiOnline()) {
                <span
                  class="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-60"
                ></span>
                <span class="relative inline-flex h-2 w-2 rounded-full bg-emerald-400"></span>
              } @else {
                <span class="relative inline-flex h-2 w-2 rounded-full bg-rose-400"></span>
              }
            </span>
            {{ apiOnline() ? 'API online' : 'API offline' }}
          </span>
        </div>
      </div>
    </header>
  `,
})
export class HeaderComponent {
  readonly stats = input<StatsResponse | null>(null);
  readonly apiOnline = input<boolean>(false);
  readonly loading = input<boolean>(false);

  protected readonly awsCount = computed(() => this.stats()?.providers?.['AWS'] ?? 0);
  protected readonly azureCount = computed(() => this.stats()?.providers?.['Azure'] ?? 0);

  protected readonly statusClasses = computed(() =>
    this.apiOnline()
      ? 'inline-flex items-center gap-2 rounded-full border px-3 py-1 text-xs font-medium transition-colors border-emerald-500/30 bg-emerald-500/10 text-emerald-300'
      : 'inline-flex items-center gap-2 rounded-full border px-3 py-1 text-xs font-medium transition-colors border-rose-500/30 bg-rose-500/10 text-rose-300',
  );
}
