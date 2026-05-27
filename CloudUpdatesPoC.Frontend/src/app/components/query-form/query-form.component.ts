import { DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';

import { QueryRequest, WorkloadProfileSummary } from '../../models/api.types';

const SUGGESTIONS: readonly string[] = [
  'Lambda',
  'API Gateway',
  'VPC',
  'S3',
  'EC2',
  'RDS',
  'DynamoDB',
  'CloudFront',
  'ECS',
  'EKS',
  'Azure Functions',
  'Cosmos DB',
  'Azure SQL',
  'App Service',
  'AKS',
];

const EXAMPLE_QUESTION = 'O que foi publicado recentemente que pode impactar meu workload?';

@Component({
  selector: 'app-query-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  template: `
    <form
      class="rounded-2xl border border-slate-800/80 bg-slate-900/60 p-6 shadow-xl shadow-black/20 backdrop-blur"
      (submit)="onSubmit($event)"
      novalidate
    >
      <fieldset class="space-y-2">
        <label for="profile-select" class="block text-sm font-semibold text-slate-200">
          Perfil de workload (demo)
        </label>
        <select
          id="profile-select"
          class="w-full rounded-xl border border-slate-700/70 bg-slate-950/60 px-3 py-2.5 text-sm text-white transition-colors focus:border-indigo-500/60 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
          [value]="selectedProfileId() ?? ''"
          (change)="onProfileChange($event)"
          [disabled]="profiles().length === 0"
        >
          @if (profiles().length === 0) {
            <option value="">Carregando perfis…</option>
          } @else {
            @for (profile of profiles(); track profile.id) {
              <option [value]="profile.id">{{ profile.name }}</option>
            }
          }
        </select>
        @if (selectedProfile(); as p) {
          <p class="text-xs leading-relaxed text-slate-500">{{ p.description }}</p>
          @if (p.snapshot; as snap) {
            <div
              class="mt-3 space-y-3 rounded-xl border border-slate-700/60 bg-slate-950/50 p-4 text-xs text-slate-400"
            >
              <div class="flex flex-wrap items-baseline justify-between gap-2">
                <p class="font-medium text-slate-300">
                  {{ snap.companyName }}
                  <span class="font-normal text-slate-500">· {{ snap.industry }}</span>
                </p>
                <p class="tabular-nums text-indigo-300/90">
                  USD {{ formatUsd(snap.monthlySpendUsd) }}/mês
                  <span class="text-slate-500">
                    ({{ snap.budgetUtilizationPercent | number: '1.0-1' }}% do budget)
                  </span>
                </p>
              </div>

              @if (snap.accountLabels.length > 0) {
                <div>
                  <p class="mb-1 font-medium text-slate-500">Contas</p>
                  <ul class="space-y-0.5">
                    @for (acc of snap.accountLabels; track acc) {
                      <li class="font-mono text-[11px] text-slate-400">{{ acc }}</li>
                    }
                  </ul>
                </div>
              }

              @if (snap.topCostServices.length > 0) {
                <div>
                  <p class="mb-1 font-medium text-slate-500">Maiores custos</p>
                  <ul class="space-y-1">
                    @for (cost of snap.topCostServices; track cost.service) {
                      <li class="flex justify-between gap-2">
                        <span>{{ cost.service }}</span>
                        <span class="shrink-0 tabular-nums text-slate-300">
                          USD {{ formatUsd(cost.monthlyUsd) }}
                          <span class="text-slate-600">({{ cost.percentOfTotal | number: '1.0-1' }}%)</span>
                        </span>
                      </li>
                    }
                  </ul>
                </div>
              }

              @if (snap.inventoryHighlights.length > 0) {
                <div>
                  <p class="mb-1 font-medium text-slate-500">Inventário (amostra)</p>
                  <ul class="list-inside list-disc space-y-0.5 text-slate-500">
                    @for (item of snap.inventoryHighlights; track item) {
                      <li>{{ item }}</li>
                    }
                  </ul>
                </div>
              }
            </div>
          }
        }
      </fieldset>

      <fieldset class="mt-6 space-y-2">
        <legend class="text-sm font-semibold text-slate-200">
          Serviços do meu workload
          <span class="ml-1 text-xs font-normal text-slate-500">
            (preenchidos pelo perfil; você pode editar — Enter ou vírgula para adicionar)
          </span>
        </legend>

        <div
          class="flex flex-wrap items-center gap-2 rounded-xl border border-slate-700/70 bg-slate-950/60 px-3 py-2 transition-colors focus-within:border-indigo-500/60 focus-within:ring-2 focus-within:ring-indigo-500/20"
          (click)="focusInput()"
        >
          @for (svc of services(); track svc) {
            <span
              class="inline-flex items-center gap-1.5 rounded-lg bg-indigo-500/15 px-2.5 py-1 text-sm font-medium text-indigo-200 ring-1 ring-inset ring-indigo-500/30"
            >
              {{ svc }}
              <button
                type="button"
                class="grid h-4 w-4 place-items-center rounded text-indigo-300 transition-colors hover:bg-indigo-500/30 hover:text-white focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-400"
                (click)="removeService(svc, $event)"
                [attr.aria-label]="'Remover ' + svc"
              >
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2.5"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  class="h-3 w-3"
                  aria-hidden="true"
                >
                  <path d="M18 6 6 18M6 6l12 12" />
                </svg>
              </button>
            </span>
          }

          <input
            #serviceField
            type="text"
            [value]="serviceInput()"
            (input)="onServiceInput($event)"
            (keydown)="onServiceKeydown($event)"
            (blur)="commitFromInput()"
            placeholder="Ex: Lambda, API Gateway…"
            class="min-w-[10rem] flex-1 border-0 bg-transparent py-1 text-sm text-white placeholder:text-slate-500 focus:outline-none focus:ring-0"
            aria-label="Adicionar serviço"
            autocomplete="off"
          />
        </div>

        <div class="flex flex-wrap gap-1.5 pt-1">
          <span class="text-xs text-slate-500">Sugestões:</span>
          @for (suggestion of suggestionList(); track suggestion) {
            <button
              type="button"
              class="rounded-md border border-slate-700/60 bg-slate-800/40 px-2 py-0.5 text-xs text-slate-300 transition-colors hover:border-indigo-500/50 hover:bg-indigo-500/10 hover:text-indigo-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-400"
              (click)="addService(suggestion)"
            >
              + {{ suggestion }}
            </button>
          }
        </div>
      </fieldset>

      <div class="mt-6 space-y-2">
        <label for="question-input" class="block text-sm font-semibold text-slate-200">
          Pergunta
        </label>
        <textarea
          id="question-input"
          [value]="question()"
          (input)="onQuestionInput($event)"
          rows="3"
          [placeholder]="placeholder"
          class="w-full resize-y rounded-xl border border-slate-700/70 bg-slate-950/60 px-3 py-2.5 text-sm text-white placeholder:text-slate-500 transition-colors focus:border-indigo-500/60 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
        ></textarea>
      </div>

      <div class="mt-6 flex items-center justify-between gap-3">
        <p class="text-xs text-slate-500">
          @if (services().length === 0) {
            Adicione ao menos 1 serviço para habilitar a análise.
          } @else {
            <span class="text-slate-400">
              {{ services().length }} {{ services().length === 1 ? 'serviço' : 'serviços' }}
              @if (selectedProfile(); as p) {
                · perfil <span class="text-indigo-300/90">{{ p.name }}</span>
              }
            </span>
          }
        </p>

        <button
          type="submit"
          [disabled]="!canSubmit()"
          class="inline-flex items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-indigo-500 to-cyan-500 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-indigo-500/20 transition-all hover:shadow-indigo-500/40 hover:brightness-110 focus:outline-none focus-visible:ring-2 focus-visible:ring-cyan-300 disabled:cursor-not-allowed disabled:from-slate-700 disabled:to-slate-700 disabled:text-slate-400 disabled:shadow-none disabled:brightness-100"
        >
          @if (loading()) {
            <svg
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2.5"
              stroke-linecap="round"
              stroke-linejoin="round"
              class="h-4 w-4 animate-spin"
              aria-hidden="true"
            >
              <path d="M21 12a9 9 0 1 1-6.219-8.56" />
            </svg>
            <span>Analisando ~{{ totalIndexed() }} updates…</span>
          } @else {
            <svg
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
              class="h-4 w-4"
              aria-hidden="true"
            >
              <path d="m5 12 5 5L20 7" />
            </svg>
            <span>Analisar</span>
          }
        </button>
      </div>
    </form>
  `,
})
export class QueryFormComponent {
  readonly loading = input<boolean>(false);
  readonly totalIndexed = input<number>(0);
  readonly profiles = input<WorkloadProfileSummary[]>([]);
  readonly submitQuery = output<QueryRequest>();

  protected readonly placeholder = EXAMPLE_QUESTION;
  protected readonly services = signal<string[]>([]);
  protected readonly serviceInput = signal('');
  protected readonly question = signal(EXAMPLE_QUESTION);
  protected readonly selectedProfileId = signal<string | null>(null);

  private readonly serviceField = viewChild<ElementRef<HTMLInputElement>>('serviceField');

  protected readonly selectedProfile = computed(() => {
    const id = this.selectedProfileId();
    if (!id) return null;
    return this.profiles().find((p) => p.id === id) ?? null;
  });

  protected readonly suggestionList = computed(() => {
    const lowercaseSelected = new Set(this.services().map((s) => s.toLowerCase()));
    return SUGGESTIONS.filter((s) => !lowercaseSelected.has(s.toLowerCase()));
  });

  protected readonly canSubmit = computed(
    () =>
      this.services().length > 0 &&
      !!this.selectedProfileId() &&
      !this.loading() &&
      this.question().trim().length > 0,
  );

  constructor() {
    effect(() => {
      const list = this.profiles();
      if (list.length > 0 && !this.selectedProfileId()) {
        this.applyProfile(list[0].id);
      }
    });
  }

  protected onProfileChange(event: Event): void {
    const id = (event.target as HTMLSelectElement).value;
    if (id) {
      this.applyProfile(id);
    }
  }

  protected onServiceInput(event: Event): void {
    this.serviceInput.set((event.target as HTMLInputElement).value);
  }

  protected onQuestionInput(event: Event): void {
    this.question.set((event.target as HTMLTextAreaElement).value);
  }

  protected onServiceKeydown(event: KeyboardEvent): void {
    const key = event.key;
    if (key === 'Enter' || key === ',') {
      event.preventDefault();
      this.commitFromInput();
      return;
    }
    if (key === 'Backspace' && this.serviceInput().length === 0 && this.services().length > 0) {
      event.preventDefault();
      this.services.update((list) => list.slice(0, -1));
    }
  }

  protected commitFromInput(): void {
    const value = this.serviceInput().replace(/,/g, '').trim();
    if (!value) {
      this.serviceInput.set('');
      return;
    }
    this.addService(value);
    this.serviceInput.set('');
  }

  protected addService(name: string): void {
    const normalized = name.trim();
    if (!normalized) return;
    const exists = this.services().some((s) => s.toLowerCase() === normalized.toLowerCase());
    if (exists) return;
    this.services.update((list) => [...list, normalized]);
  }

  protected removeService(name: string, event: Event): void {
    event.stopPropagation();
    this.services.update((list) => list.filter((s) => s !== name));
  }

  protected focusInput(): void {
    this.serviceField()?.nativeElement.focus();
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    if (!this.canSubmit()) return;
    const profileId = this.selectedProfileId();
    this.submitQuery.emit({
      profileId: profileId ?? undefined,
      services: [...this.services()],
      question: this.question().trim(),
    });
  }

  protected formatUsd(value: number): string {
    if (value >= 1_000) {
      return `${(value / 1_000).toFixed(1)}k`;
    }
    return value.toLocaleString('en-US', { maximumFractionDigits: 0 });
  }

  private applyProfile(id: string): void {
    const profile = this.profiles().find((p) => p.id === id);
    if (!profile) return;
    this.selectedProfileId.set(id);
    this.services.set([...profile.services]);
  }
}
