import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DatePipe } from '@angular/common';

import { Source } from '../../models/api.types';

interface RenderedSource {
  readonly index: number;
  readonly title: string;
  readonly url: string;
  readonly provider: string;
  readonly publishedAt: string;
  readonly scorePct: number;
  readonly scoreLabel: string;
  readonly providerClasses: string;
  readonly scoreClasses: string;
}

@Component({
  selector: 'app-sources-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  template: `
    <aside
      class="rounded-2xl border border-slate-800/80 bg-slate-900/60 p-6 shadow-xl shadow-black/20 backdrop-blur"
    >
      <header class="mb-4 flex items-center justify-between">
        <div class="flex items-center gap-2">
          <div
            class="grid h-8 w-8 place-items-center rounded-lg bg-slate-800 text-slate-300"
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
              class="h-4 w-4"
            >
              <path d="M4 4h16v16H4z" />
              <path d="M4 9h16" />
              <path d="M9 4v16" />
            </svg>
          </div>
          <div>
            <h2 class="text-base font-semibold text-white">Sources</h2>
            <p class="text-xs text-slate-500">Updates usados como contexto</p>
          </div>
        </div>
        <span
          class="rounded-full bg-slate-800/70 px-2.5 py-0.5 text-xs font-medium text-slate-300"
        >
          {{ renderedSources().length }}
        </span>
      </header>

      @if (renderedSources().length === 0) {
        <p class="rounded-xl border border-dashed border-slate-700/70 bg-slate-950/40 p-4 text-sm text-slate-400">
          Nenhum update relevante foi encontrado no índice para essa pergunta. Tente serviços
          diferentes ou refine a pergunta.
        </p>
      } @else {
        <ol class="space-y-3">
          @for (s of renderedSources(); track s.url; let i = $index) {
            <li
              class="group relative rounded-xl border border-slate-800/70 bg-slate-950/40 p-4 transition-all hover:-translate-y-0.5 hover:border-slate-700 hover:bg-slate-900/60"
            >
              <div class="flex items-start justify-between gap-3">
                <span
                  class="inline-flex items-center gap-1 rounded-md px-2 py-0.5 text-[0.6875rem] font-semibold uppercase tracking-wide"
                  [class]="s.providerClasses"
                >
                  <span class="opacity-70">[{{ s.index }}]</span>
                  {{ s.provider }}
                </span>
                <span
                  class="inline-flex items-center gap-1 text-[0.6875rem] font-semibold"
                  [class]="s.scoreClasses"
                  [attr.title]="'Score: ' + s.scoreLabel"
                >
                  {{ s.scoreLabel }}%
                </span>
              </div>

              <a
                [href]="s.url"
                target="_blank"
                rel="noopener noreferrer"
                class="mt-2 block text-sm font-medium text-slate-100 underline-offset-4 transition-colors group-hover:text-white hover:underline focus:outline-none focus-visible:underline focus-visible:ring-2 focus-visible:ring-indigo-400"
              >
                {{ s.title }}
              </a>

              <div class="mt-3 flex items-center justify-between gap-3">
                <time class="text-xs text-slate-500" [attr.datetime]="s.publishedAt">
                  {{ s.publishedAt | date: 'longDate' : undefined : 'pt-BR' }}
                </time>
              </div>

              <div
                class="mt-2 h-1 w-full overflow-hidden rounded-full bg-slate-800"
                [attr.aria-label]="'Relevância: ' + s.scoreLabel + '%'"
                role="progressbar"
                [attr.aria-valuenow]="s.scorePct"
                aria-valuemin="0"
                aria-valuemax="100"
              >
                <div
                  class="h-full rounded-full bg-gradient-to-r from-indigo-500 to-cyan-400 transition-all"
                  [style.width.%]="s.scorePct"
                ></div>
              </div>
            </li>
          }
        </ol>
      }
    </aside>
  `,
})
export class SourcesPanelComponent {
  readonly sources = input<readonly Source[]>([]);

  protected readonly renderedSources = computed<readonly RenderedSource[]>(() => {
    const list = [...this.sources()]
      .map((s, idx) => ({ source: s, originalIndex: idx + 1 }))
      .sort((a, b) => b.source.score - a.source.score);

    return list.map(({ source, originalIndex }) => {
      const pct = clampPercent(source.score * 100);
      const provider = source.provider ?? 'Outro';
      return {
        index: originalIndex,
        title: source.title || '(sem título)',
        url: source.url,
        provider,
        publishedAt: source.publishedAt,
        scorePct: pct,
        scoreLabel: pct.toFixed(0),
        providerClasses: providerStyle(provider),
        scoreClasses: scoreStyle(pct),
      };
    });
  });
}

function clampPercent(value: number): number {
  if (Number.isNaN(value)) return 0;
  return Math.max(0, Math.min(100, value));
}

function providerStyle(provider: string): string {
  const key = provider.toLowerCase();
  if (key === 'aws') {
    return 'bg-amber-500/15 text-amber-300 ring-1 ring-inset ring-amber-500/30';
  }
  if (key === 'azure') {
    return 'bg-sky-500/15 text-sky-300 ring-1 ring-inset ring-sky-500/30';
  }
  return 'bg-slate-700/40 text-slate-300 ring-1 ring-inset ring-slate-600/40';
}

function scoreStyle(pct: number): string {
  if (pct >= 70) return 'text-emerald-300';
  if (pct >= 40) return 'text-amber-300';
  return 'text-slate-400';
}
