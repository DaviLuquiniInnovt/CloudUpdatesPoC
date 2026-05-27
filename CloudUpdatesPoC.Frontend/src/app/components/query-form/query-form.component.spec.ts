import { ComponentFixture, TestBed } from '@angular/core/testing';

import { QueryFormComponent } from './query-form.component';
import { WorkloadProfileSummary } from '../../models/api.types';

const MOCK_SNAPSHOT = {
  companyName: 'NebulaPay Tecnologia Ltda.',
  industry: 'Fintech',
  monthlySpendUsd: 48_200,
  monthlyBudgetUsd: 52_000,
  budgetUtilizationPercent: 92.7,
  accountLabels: ['AWS: nebulapay-prod (782910345678)'],
  primaryRegions: ['us-east-1'],
  topCostServices: [
    { service: 'AWS Lambda', monthlyUsd: 10_560, percentOfTotal: 21.9, notes: '142 funções' },
  ],
  inventoryHighlights: ['Lambda: 142 funções'],
  complianceFrameworks: ['PCI-DSS', 'LGPD'],
};

const MOCK_PROFILES: WorkloadProfileSummary[] = [
  {
    id: 'serverless-saas-aws',
    name: 'SaaS Serverless (AWS)',
    description: 'Demo serverless',
    services: ['Lambda', 'API Gateway', 'S3'],
    snapshot: MOCK_SNAPSHOT,
  },
  {
    id: 'kubernetes-platform-azure',
    name: 'Plataforma Kubernetes (Azure)',
    description: 'Demo AKS',
    services: ['AKS', 'Azure Monitor'],
    snapshot: { ...MOCK_SNAPSHOT, companyName: 'Horizon Retail', monthlySpendUsd: 67_400 },
  },
];

describe('QueryFormComponent', () => {
  let fixture: ComponentFixture<QueryFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QueryFormComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(QueryFormComponent);
    fixture.componentRef.setInput('profiles', MOCK_PROFILES);
    fixture.detectChanges();
  });

  it('should prefill services when the first profile loads', () => {
    const select = fixture.nativeElement.querySelector(
      '#profile-select',
    ) as HTMLSelectElement;
    expect(select.value).toBe('serverless-saas-aws');

    const chips = fixture.nativeElement.querySelectorAll(
      'span.inline-flex.items-center',
    );
    expect(chips.length).toBe(3);
    expect(chips[0].textContent?.trim()).toContain('Lambda');
  });

  it('should replace services when profile changes', () => {
    const select = fixture.nativeElement.querySelector(
      '#profile-select',
    ) as HTMLSelectElement;
    select.value = 'kubernetes-platform-azure';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const chipEls = fixture.nativeElement.querySelectorAll(
      'span.inline-flex.items-center',
    ) as NodeListOf<HTMLElement>;
    const chips = Array.from(chipEls).map((el) => el.textContent?.trim() ?? '');

    expect(chips.some((t) => t.includes('AKS'))).toBe(true);
    expect(chips.some((t) => t.includes('Lambda'))).toBe(false);
  });

  it('should emit profileId and services on submit', () => {
    const emitted: unknown[] = [];
    fixture.componentInstance.submitQuery.subscribe((payload) => emitted.push(payload));

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(emitted.length).toBe(1);
    expect(emitted[0]).toEqual({
      profileId: 'serverless-saas-aws',
      services: ['Lambda', 'API Gateway', 'S3'],
      question: expect.any(String),
    });
  });
});
