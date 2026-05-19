import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Marked } from 'marked';

const marked = new Marked({ gfm: true, breaks: true });

@Component({
  selector: 'app-answer-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      class="rounded-2xl border border-slate-800/80 bg-slate-900/60 p-6 shadow-xl shadow-black/20 backdrop-blur"
    >
      <header class="mb-4 flex items-center gap-2">
        <div
          class="grid h-8 w-8 place-items-center rounded-lg bg-gradient-to-br from-indigo-500 to-cyan-500 text-white"
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
            <path d="M12 2a3 3 0 0 0-3 3v1h6V5a3 3 0 0 0-3-3Z" />
            <path d="M6 6h12v3a6 6 0 0 1-12 0V6Z" />
            <path d="M12 15v4" />
            <path d="M8 22h8" />
          </svg>
        </div>
        <div>
          <h2 class="text-base font-semibold text-white">Análise</h2>
          <p class="text-xs text-slate-500">Gerada a partir dos updates indexados</p>
        </div>
      </header>

      <div class="markdown-body text-sm leading-relaxed text-slate-200" [innerHTML]="renderedAnswer()"></div>

      <footer class="mt-6 flex justify-end border-t border-slate-800/70 pt-4">
        <button
          type="button"
          (click)="reset.emit()"
          class="inline-flex items-center gap-2 rounded-lg border border-slate-700/70 bg-slate-800/40 px-3 py-1.5 text-xs font-medium text-slate-300 transition-colors hover:border-indigo-500/40 hover:bg-indigo-500/10 hover:text-indigo-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-400"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
            class="h-3.5 w-3.5"
            aria-hidden="true"
          >
            <path d="M21 12a9 9 0 1 1-3-6.7" />
            <path d="M21 3v6h-6" />
          </svg>
          Nova pergunta
        </button>
      </footer>
    </article>
  `,
})
export class AnswerCardComponent {
  readonly answer = input<string>('');
  readonly reset = output<void>();

  protected readonly renderedAnswer = computed<string>(() => {
    const raw = this.answer();
    if (!raw) return '';
    return marked.parse(raw, { async: false }) as string;
  });
}
