import { Component, OnDestroy, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminAward } from '../../../core/awards/awards.models';
import { AwardsService } from '../../../core/awards/awards.service';

const IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;

@Component({
  selector: 'app-award-form-modal',
  imports: [ReactiveFormsModule],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
        class="panel flex max-h-[90vh] w-full max-w-xl flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 class="text-lg font-bold text-ink">{{ award() ? 'Edit award' : 'New award' }}</h2>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="flex flex-col gap-5 overflow-y-auto px-6 py-5">
          <div class="flex items-center gap-4">
            <span class="flex h-24 w-24 shrink-0 items-center justify-center overflow-hidden rounded-full bg-gradient-to-br from-sol-200 to-sol-400">
              @if (previewUrl(); as url) {
                <img [src]="url" alt="" class="h-full w-full object-cover" />
              } @else {
                <i class="ti ti-award text-5xl text-atlantico-900" aria-hidden="true"></i>
              }
            </span>
            <div class="flex flex-col gap-2">
              <div class="flex flex-wrap gap-2">
                <label class="btn-secondary flex cursor-pointer items-center gap-2">
                  <i class="ti ti-upload" aria-hidden="true"></i>{{ previewUrl() ? 'Change image' : 'Upload image' }}
                  <input type="file" accept="image/jpeg,image/png,image/webp" class="hidden" (change)="onFileSelected($event)" />
                </label>
                @if (previewUrl()) {
                  <button type="button" class="btn-secondary flex items-center gap-2" (click)="removeImage()">
                    <i class="ti ti-trash" aria-hidden="true"></i>Remove
                  </button>
                }
              </div>
              @if (imageError(); as message) {
                <p class="field-error">{{ message }}</p>
              } @else {
                <p class="text-xs text-ink-muted">Square JPG, PNG or WEBP, up to 5 MB. Without an image a medal icon is shown.</p>
              }
            </div>
          </div>

          <div>
            <label for="awardName" class="field-label">Name</label>
            <input id="awardName" formControlName="name" maxlength="80" class="field-input" placeholder="e.g. Litoral Founders" />
            @if (form.controls.name.invalid && form.controls.name.touched) {
              <p class="field-error">Enter the award name.</p>
            }
          </div>

          <div>
            <label for="awardDescription" class="field-label">Description</label>
            <textarea id="awardDescription" formControlName="description" maxlength="400" rows="3" class="field-input"
              placeholder="Shown to the pilots, e.g. For the first pilots who flew with Litoral."></textarea>
            @if (form.controls.description.invalid && form.controls.description.touched) {
              <p class="field-error">Enter the award description.</p>
            }
          </div>

          <label class="flex cursor-pointer items-center gap-3">
            <input type="checkbox" formControlName="isActive" class="h-4 w-4 accent-primary" />
            <span>
              <span class="block font-medium text-ink">Active</span>
              <span class="block text-xs text-ink-muted">Inactive awards stay with the pilots who have them but can't be granted.</span>
            </span>
          </label>
        </div>

        @if (formError(); as message) {
          <p class="border-t border-danger/30 bg-danger/10 px-6 py-3 text-sm text-danger">
            <i class="ti ti-alert-circle mr-1" aria-hidden="true"></i>{{ message }}
          </p>
        }

        <div class="flex justify-end gap-2 border-t border-line px-6 py-4">
          <button type="button" class="btn-secondary" (click)="close()">Cancel</button>
          <button type="submit" class="btn-primary px-5" [disabled]="saving()">{{ saving() ? 'Loading...' : 'Save' }}</button>
        </div>
      </form>
    </div>
  `
})
export class AwardFormModal implements OnInit, OnDestroy {
  private readonly api = inject(AwardsService);

  readonly award = input<AdminAward | null>(null);
  readonly saved = output<AdminAward>();
  readonly closed = output<void>();

  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly imageError = signal<string | null>(null);
  private readonly imageFile = signal<File | null>(null);
  private readonly imageRemoved = signal(false);
  private readonly localPreview = signal<string | null>(null);

  protected readonly previewUrl = computed(() => {
    if (this.localPreview()) return this.localPreview();
    if (this.imageRemoved()) return null;
    return this.award()?.imageUrl ?? null;
  });

  protected readonly form = inject(FormBuilder).nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(80)]],
    description: ['', [Validators.required, Validators.maxLength(400)]],
    isActive: [true]
  });

  ngOnInit(): void {
    const award = this.award();
    if (award) this.form.setValue({ name: award.name, description: award.description, isActive: award.isActive });
  }

  ngOnDestroy(): void {
    this.revokePreview();
  }

  protected onFileSelected(event: Event): void {
    const element = event.target as HTMLInputElement;
    const file = element.files?.[0];
    element.value = '';
    if (!file) return;

    if (!IMAGE_TYPES.includes(file.type)) {
      this.imageError.set('Use a JPG, PNG or WEBP image.');
      return;
    }
    if (file.size > MAX_IMAGE_BYTES) {
      this.imageError.set('The image must be 5 MB or smaller.');
      return;
    }

    this.imageError.set(null);
    this.revokePreview();
    this.imageFile.set(file);
    this.imageRemoved.set(false);
    this.localPreview.set(URL.createObjectURL(file));
  }

  protected removeImage(): void {
    this.revokePreview();
    this.imageFile.set(null);
    this.imageRemoved.set(true);
  }

  protected close(): void {
    if (!this.saving()) this.closed.emit();
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.formError.set(null);

    try {
      const v = this.form.getRawValue();
      const payload = { name: v.name.trim(), description: v.description.trim(), isActive: v.isActive };
      const current = this.award();
      let result = current ? await this.api.update(current.id, payload) : await this.api.create(payload);

      const file = this.imageFile();
      if (file) result = await this.api.uploadImage(result.id, file);
      else if (this.imageRemoved() && result.imageUrl) result = await this.api.removeImage(result.id);

      this.saved.emit(result);
    } catch (error) {
      this.formError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }

  private revokePreview(): void {
    const url = this.localPreview();
    if (url) URL.revokeObjectURL(url);
    this.localPreview.set(null);
  }
}