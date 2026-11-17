import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { RULE_TONE_CLASS, RULE_TONE_ICON, ScoringRule, ScoringSection, ruleTone, ruleValue } from '../../core/scoring/scoring.models';
import { ScoringService } from '../../core/scoring/scoring.service';
import { PointsStatementView } from './points-statement';

type Tab = 'statement' | 'scoring';

@Component({
  selector: 'app-points',
  imports: [PointsStatementView],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">Points</h1>
          <p class="text-ink-muted">{{ tab() === 'statement' ? 'Every point you earned and spent.' : 'How every flight is scored. Rejected flights still count as flight hours.' }}</p>
        </div>
        <div class="flex rounded-lg border border-line p-0.5 text-sm font-semibold">
          @for (t of tabs; track t.value) {
            <button type="button" class="flex items-center gap-1.5 rounded-md px-4 py-1.5" (click)="setTab(t.value)"
              [class]="tab() === t.value ? 'bg-primary text-on-primary' : 'text-ink-muted hover:text-ink'">
              <i class="ti {{ t.icon }}" aria-hidden="true"></i>{{ t.label }}
            </button>
          }
        </div>
      </div>

      @if (tab() === 'statement') {
        <app-points-statement />
      } @else if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (loadError()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else {
        <div class="grid gap-6 lg:grid-cols-2">
          @for (section of sections(); track section.category) {
            <article class="panel flex flex-col gap-4 p-6" [class.lg:col-span-2]="section.category === 'Rejection'">
              <div>
                <h2 class="text-xl font-bold uppercase tracking-wide text-ink">{{ section.title }}</h2>
                @if (section.subtitle) {
                  <p class="text-sm text-ink-muted">{{ section.subtitle }}</p>
                }
              </div>

              <table class="w-full border border-line text-left text-sm">
                <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
                  <tr>
                    <th class="w-40 border-r border-line px-4 py-3 font-semibold">Points</th>
                    <th class="px-4 py-3 font-semibold">Description</th>
                  </tr>
                </thead>
                <tbody>
                  @for (rule of section.rules; track rule.id) {
                    <tr class="border-b border-line last:border-0">
                      <td class="border-r border-line px-4 py-3 font-bold" [class]="toneClass(rule)">
                        <span class="flex items-center gap-2 whitespace-nowrap">
                          <i class="ti {{ toneIcon(rule) }} text-lg" aria-hidden="true"></i>{{ value(rule) }}
                        </span>
                      </td>
                      <td class="px-4 py-3 text-ink">
                        {{ rule.description }}
                        @if (rule.graceSeconds > 0) {
                          <span class="ml-1 text-xs text-ink-muted">· after {{ rule.graceSeconds }} s</span>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </article>
          }
        </div>
      }
    </section>
  `
})
export class Points implements OnInit {
  private readonly api = inject(ScoringService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly tabs: { value: Tab; label: string; icon: string }[] = [
    { value: 'statement', label: 'My statement', icon: 'ti-receipt' },
    { value: 'scoring', label: 'How scoring works', icon: 'ti-list-details' }
  ];
  protected readonly tab = signal<Tab>(this.route.snapshot.queryParamMap.get('tab') === 'scoring' ? 'scoring' : 'statement');

  protected readonly sections = signal<ScoringSection[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);

  ngOnInit(): void {
    this.load();
  }

  protected setTab(value: Tab): void {
    this.tab.set(value);
    this.router.navigate([], { queryParams: { tab: value === 'scoring' ? 'scoring' : null }, replaceUrl: true });
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);

    try {
      this.sections.set(await this.api.table());
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected value(rule: ScoringRule): string {
    return ruleValue(rule);
  }

  protected toneClass(rule: ScoringRule): string {
    return RULE_TONE_CLASS[ruleTone(rule)];
  }

  protected toneIcon(rule: ScoringRule): string {
    return RULE_TONE_ICON[ruleTone(rule)];
  }
}