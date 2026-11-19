import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminDownloadItem, DOWNLOAD_CATEGORIES, DownloadCategory, formatBytes } from '../../../core/downloads/downloads.models';
import { DownloadsService } from '../../../core/downloads/downloads.service';

const MAX_FILE_BYTES = 20 * 1024 * 1024;
const EXTENSIONS = ['.pdf', '.zip', '.7z', '.rar', '.txt', '.png', '.jpg', '.jpeg'];

type Source = 'file' | 'link';

@Component({
  selector: 'app-download-form-modal',
  imports: [ReactiveFormsModule],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
        class="panel flex max-h-[90vh] w-full max-w-2xl flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 class="text-lg font-bold text-ink">{{ item() ? 'Edit download' : 'New download' }}</h2>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="grid gap-5 overflow-y-auto px-6 py-5 sm:grid-cols-2">
          <div class="sm:col-span-2">
            <label for="dlTitle" class="field-label">Title</label>
            <input id="dlTitle" formControlName="title" maxlength="120" class="field-input" placeholder="e.g. Litoral livery for the PMDG 737-800" />
            @if (form.controls.title.invalid && form.controls.title.touched) {
              <p class="field-error">Enter the title.</p>
            }
          </div>

          <div>
            <label for="dlCategory" class="field-label">Category</label>
            <select id="dlCategory" formControlName="category" class="field-input">
              @for (c of categories; track c.value) {
                <option [value]="c.value">{{ c.label }}</option>
              }
            </select>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label for="dlType" class="field-label">Aircraft</label>
              <input id="dlType" formControlName="aircraftType" maxlength="4" class="field-input uppercase" placeholder="B738" />
            </div>
            <div>
              <label for="dlVersion" class="field-label">Version</label>
              <input id="dlVersion" formControlName="version" maxlength="30" class="field-input" placeholder="1.0" />
            </div>
          </div>

          <div class="sm:col-span-2">
            <label for="dlDescription" class="field-label">Description</label>
            <textarea id="dlDescription" formControlName="description" maxlength="500" rows="3" class="field-input"
              placeholder="Optional, e.g. which simulator and add-on it is for"></textarea>
          </div>

          <fieldset class="sm:col-span-2">
            <legend class="field-label">Source</legend>
            <div class="flex rounded-lg border border-line p-0.5 text-sm font-semibold">
              @for (s of sources; track s.value) {
                <button type="button" class="flex flex-1 items-center justify-center gap-2 rounded-md px-3 py-1.5"
                  [class]="source() === s.value ? 'bg-primary text-on-primary' : 'text-ink-muted hover:text-ink'" (click)="source.set(s.value)">
                  <i class="ti {{ s.icon }}" aria-hidden="true"></i>{{ s.label }}
                </button>
              }
            </div>
          </fieldset>

          @if (source() === 'file') {
            <div class="sm:col-span-2 flex flex-col gap-2">
              <label class="flex cursor-pointer items-center gap-3 rounded-lg border border-dashed border-line px-4 py-4 hover:bg-surface-alt">
                <i class="ti ti-upload text-2xl text-primary" aria-hidden="true"></i>
                <span class="min-w-0 flex-1">
                  @if (file(); as f) {
                    <span class="block truncate font-medium text-ink">{{ f.name }}</span>
                    <span class="text-xs text-ink-muted">{{ size(f.size) }}</span>
                  } @else if (item()?.isFile) {
                    <span class="block truncate font-medium text-ink">{{ item()!.fileName }}</span>
                    <span class="text-xs text-ink-muted">{{ size(item()!.sizeBytes) }} · choose a file to replace it</span>
                  } @else {
                    <span class="block font-medium text-ink">Choose a file</span>
                    <span class="text-xs text-ink-muted">PDF, ZIP, 7Z, RAR, TXT, PNG or JPG, up to 20 MB</span>
                  }
                </span>
                <input type="file" class="hidden" [accept]="accept" (change)="onFileSelected($event)" />
              </label>
              @if (fileError(); as message) {
                <p class="field-error">{{ message }}</p>
              }
            </div>
          } @else {
            <div class="sm:col-span-2">
              <label for="dlUrl" class="field-label">Link</label>
              <input id="dlUrl" formControlName="externalUrl" maxlength="500" class="field-input" placeholder="https://flightsim.to/..." />
              <p class="mt-1 text-xs text-ink-muted">Use a link for files bigger than 20 MB (Google Drive, Mega, flightsim.to).</p>
            </div>
          }
        </div>

        @if (formError(); as message) {
          <p class="border-t border-danger/30 bg-danger/10 px-6 py-3 text-sm text-danger">
            <i class="ti ti-alert-circle mr-1" aria-hidden="true"></i>{{ message }}
          </p>
        }

        <div class="flex justify-end gap-2 border-t border-line px-6 py-4">
          <button type="button" class="btn-secondary" (click)="close()">Cancel</button>
          <button type="submit" class="btn-primary px-5" [disabled]="saving()">{{ saving() ? 'Uploading...' : 'Save' }}</button>
        </div>
      </form>
    </div>
  `
})
export class DownloadFormModal implements OnInit {
  private readonly api = inject(DownloadsService);

  readonly item = input<AdminDownloadItem | null>(null);
  readonly saved = output<AdminDownloadItem>();
  readonly closed = output<void>();

  protected readonly categories = DOWNLOAD_CATEGORIES;
  protected readonly accept = EXTENSIONS.join(',');
  protected readonly sources: { value: Source; label: string; icon: string }[] = [
    { value: 'file', label: 'Upload file', icon: 'ti-file-upload' },
    { value: 'link', label: 'External link', icon: 'ti-link' }
  ];

  protected readonly source = signal<Source>('file');
  protected readonly file = signal<File | null>(null);
  protected readonly fileError = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(120)]],
    category: ['Documents' as DownloadCategory],
    aircraftType: [''],
    version: [''],
    description: [''],
    externalUrl: ['']
  });

  ngOnInit(): void {
    const item = this.item();
    if (!item) return;

    this.source.set(item.isFile ? 'file' : 'link');
    this.form.setValue({
      title: item.title,
      category: item.category,
      aircraftType: item.aircraftType ?? '',
      version: item.version ?? '',
      description: item.description ?? '',
      externalUrl: item.externalUrl ?? ''
    });
  }

  protected size(bytes: number): string {
    return formatBytes(bytes);
  }

  protected onFileSelected(event: Event): void {
    const element = event.target as HTMLInputElement;
    const file = element.files?.[0];
    element.value = '';
    if (!file) return;

    const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
    if (!EXTENSIONS.includes(extension)) {
      this.fileError.set('Use a PDF, ZIP, 7Z, RAR, TXT, PNG or JPG file.');
      return;
    }
    if (file.size > MAX_FILE_BYTES) {
      this.fileError.set('The file must be 20 MB or smaller. Use an external link instead.');
      return;
    }

    this.fileError.set(null);
    this.file.set(file);
  }

  protected close(): void {
    if (!this.saving()) this.closed.emit();
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const url = v.externalUrl.trim();
    const file = this.source() === 'file' ? this.file() : null;

    if (this.source() === 'file' && !file && !this.item()?.isFile) {
      this.formError.set('Choose a file to upload.');
      return;
    }
    if (this.source() === 'link' && !url.startsWith('https://')) {
      this.formError.set('Enter a link that starts with https://.');
      return;
    }

    this.saving.set(true);
    this.formError.set(null);

    try {
      const payload = {
        title: v.title.trim(),
        category: v.category,
        aircraftType: v.aircraftType.trim().toUpperCase() || null,
        version: v.version.trim() || null,
        description: v.description.trim() || null,
        externalUrl: this.source() === 'link' ? url : null,
        file
      };
      const current = this.item();
      this.saved.emit(current ? await this.api.update(current.id, payload) : await this.api.create(payload));
    } catch (error) {
      this.formError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }
}