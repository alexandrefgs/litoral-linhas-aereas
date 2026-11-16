import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AdminScoringService } from '../../../core/admin/admin-scoring.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import {
  RULE_TONE_CLASS, RULE_TONE_ICON, ScoringCatalog, ScoringRule, ruleTone, ruleValue
} from '../../../core/scoring/scoring.models';
import { ScoringRuleFormModal } from './scoring-rule-form-modal';

@Component({
  selector: 'app-admin-scoring',
  imports: [ScoringRuleFormModal],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">Scoring</h1>
          <p class="text-ink-muted">Rules used to score and review every flight. Pilots see the active ones on the Points page.</p>
        </div>
        <button type="button" class="btn-primary gap-2 px-4 py-2.5" (click)="openCreate(null)" [disabled]="!catalog()">
          <i class="ti ti-plus text-lg" aria-hidden="true"></i>New rule
        </button>
      </div>

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (loadError()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else {
        @for (section of sections(); track section.category) {
          <div class="panel overflow-x-auto">
            <div class="flex items-center justify-between gap-4 border-b border-line px-4 py-3">
              <div>
                <h2 class="font-bold text-ink">{{ section.title }}</h2>
                @if (section.subtitle) {
                  <p class="text-xs text-ink-muted">{{ section.subtitle }}</p>
                }
              </div>
              <button type="button" class="icon-btn h-9 w-9 text-lg" aria-label="Add rule to this category"
                (click)="openCreate(section.category)">
                <i class="ti ti-plus" aria-hidden="true"></i>
              </button>
            </div>

            @if (section.rules.length === 0) {
              <p class="px-4 py-4 text-sm text-ink-muted">No rules in this category.</p>
            } @else {
              <table class="w-full text-left text-sm">
                <tbody>
                  @for (rule of section.rules; track rule.id) {
                    <tr class="border-b border-line last:border-0 hover:bg-surface-alt/50" [class.opacity-50]="!rule.isActive">
                      <td class="w-36 px-4 py-3 font-bold" [class]="toneClass(rule)">
                        <span class="flex items-center gap-2 whitespace-nowrap">
                          <i class="ti {{ toneIcon(rule) }} text-lg" aria-hidden="true"></i>{{ value(rule) }}
                        </span>
                      </td>
                      <td class="px-4 py-3">
                        <p class="text-ink">{{ rule.description }}</p>
                        <p class="text-xs text-ink-muted">{{ details(rule) }}</p>
                      </td>
                      <td class="w-24 px-4 py-3">
                        @if (!rule.isActive) {
                          <span class="rounded-full bg-surface-alt px-2.5 py-1 text-xs font-semibold text-ink-muted">Inactive</span>
                        }
                      </td>
                      <td class="w-48 px-4 py-3">
                        @if (confirmDelete() === rule.id) {
                          <div class="flex items-center justify-end gap-2">
                            <span class="text-xs text-ink-muted">Delete?</span>
                            <button type="button" class="rounded-md bg-danger px-3 py-1 text-xs font-medium text-white hover:opacity-90"
                              (click)="remove(rule)">Delete</button>
                            <button type="button" class="btn-secondary px-3 py-1 text-xs" (click)="confirmDelete.set(null)">Cancel</button>
                          </div>
                        } @else {
                          <div class="flex justify-end gap-1">
                            <button type="button" class="icon-btn h-9 w-9 text-lg" aria-label="Edit rule" (click)="openEdit(rule)">
                              <i class="ti ti-pencil" aria-hidden="true"></i>
                            </button>
                            <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" aria-label="Delete rule"
                              (click)="confirmDelete.set(rule.id)">
                              <i class="ti ti-trash" aria-hidden="true"></i>
                            </button>
                          </div>
                        }
                        @if (rowError()?.id === rule.id) {
                          <p class="mt-1 text-right text-xs text-danger">{{ rowError()?.message }}</p>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            }
          </div>
        }
      }
    </div>

    @if (modalOpen() && catalog(); as catalog) {
      <app-scoring-rule-form-modal [catalog]="catalog" [rule]="editing()" [category]="newCategory()"
        (saved)="onSaved($event)" (closed)="modalOpen.set(false)" />
    }
  `
})
export class AdminScoring implements OnInit {
  private readonly api = inject(AdminScoringService);

  protected readonly catalog = signal<ScoringCatalog | null>(null);
  protected readonly rules = signal<ScoringRule[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly modalOpen = signal(false);
  protected readonly editing = signal<ScoringRule | null>(null);
  protected readonly newCategory = signal<string | null>(null);
  protected readonly confirmDelete = signal<string | null>(null);
  protected readonly rowError = signal<{ id: string; message: string } | null>(null);

  protected readonly sections = computed(() => {
    const catalog = this.catalog();
    if (!catalog) return [];

    return catalog.categories.map(c => ({
      ...c,
      rules: this.rules()
        .filter(r => r.category === c.category)
        .sort((a, b) => a.order - b.order || a.description.localeCompare(b.description))
    }));
  });

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);

    try {
      const [catalog, rules] = await Promise.all([this.api.catalog(), this.api.rules()]);
      this.catalog.set(catalog);
      this.rules.set(rules);
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected openCreate(category: string | null): void {
    this.editing.set(null);
    this.newCategory.set(category);
    this.modalOpen.set(true);
  }

  protected openEdit(rule: ScoringRule): void {
    this.editing.set(rule);
    this.newCategory.set(null);
    this.modalOpen.set(true);
  }

  protected onSaved(rule: ScoringRule): void {
    this.rules.update(list => [...list.filter(r => r.id !== rule.id), rule]);
    this.modalOpen.set(false);
  }

  protected async remove(rule: ScoringRule): Promise<void> {
    this.rowError.set(null);

    try {
      await this.api.delete(rule.id);
      this.rules.update(list => list.filter(r => r.id !== rule.id));
    } catch (error) {
      this.rowError.set({ id: rule.id, message: apiErrorMessage(error) ?? 'Could not delete the rule.' });
    } finally {
      this.confirmDelete.set(null);
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

  protected details(rule: ScoringRule): string {
    const check = this.catalog()?.checks.find(c => c.check === rule.check);
    const parts = [check?.label ?? rule.check];

    if (check?.minLabel && rule.min !== null) parts.push(`${check.minLabel}: ${rule.min}`);
    if (check?.maxLabel && rule.max !== null) parts.push(`${check.maxLabel}: ${rule.max}`);
    if (check?.textLabel && rule.text) parts.push(`${check.textLabel}: ${rule.text}`);
    if (rule.aircraftTypes) parts.push(rule.aircraftTypes.replaceAll(',', ', '));
    if (rule.graceSeconds > 0) parts.push(`after ${rule.graceSeconds} s`);

    return parts.join(' · ');
  }
}