import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';

export interface UploadedFile {
  file: File;
  previewUrl: string | null;
}

@Component({
  selector: 'app-file-upload',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass],
  template: `
    <!-- Drop zone -->
    <div
      class="border-2 border-dashed rounded-xl p-6 text-center transition-colors cursor-pointer"
      [ngClass]="isDragging() ? 'border-navy bg-navy/5' : 'border-border hover:border-navy/50 hover:bg-bg'"
      (click)="fileInput.click()"
      (dragover)="onDragOver($event)"
      (dragleave)="onDragLeave()"
      (drop)="onDrop($event)"
      role="button"
      tabindex="0"
      aria-label="Upload files — click or drag and drop"
      (keydown.enter)="fileInput.click()"
      (keydown.space)="fileInput.click()"
    >
      <span class="text-3xl" aria-hidden="true">📁</span>
      <p class="text-sm font-medium text-dark mt-2">
        {{ isDragging() ? 'Drop files here' : 'Click to upload or drag & drop' }}
      </p>
      <p class="text-xs text-muted mt-1">{{ hint }}</p>

      <input
        #fileInput
        type="file"
        class="hidden"
        [accept]="accept"
        [multiple]="multiple"
        (change)="onFileInputChange($event)"
        aria-hidden="true"
      />
    </div>

    <!-- Preview grid -->
    @if (files().length) {
      <div class="mt-4 grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-3">
        @for (f of files(); track f.file.name) {
          <div class="relative group rounded-lg overflow-hidden border border-border bg-bg aspect-square">
            @if (f.previewUrl) {
              <img
                [src]="f.previewUrl"
                [alt]="f.file.name"
                class="w-full h-full object-cover"
                loading="lazy"
              />
            } @else {
              <div class="w-full h-full flex items-center justify-center">
                <span class="text-2xl" aria-hidden="true">📄</span>
              </div>
            }
            <!-- Remove button -->
            <button
              type="button"
              class="absolute top-1 right-1 w-5 h-5 rounded-full bg-black/60 text-white text-xs opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center"
              (click)="removeFile(f); $event.stopPropagation()"
              [attr.aria-label]="'Remove ' + f.file.name"
            >
              ✕
            </button>
            <p class="absolute bottom-0 inset-x-0 bg-black/50 text-white text-[9px] px-1 py-0.5 truncate">
              {{ f.file.name }}
            </p>
          </div>
        }
      </div>
    }

    @if (error()) {
      <p class="text-xs text-red mt-2">{{ error() }}</p>
    }
  `,
})
export class FileUploadComponent {
  @Input() accept = 'image/*';
  @Input() multiple = true;
  @Input() maxSizeMb = 5;
  @Input() hint = 'PNG, JPG up to 5 MB';

  @Output() filesChange = new EventEmitter<File[]>();

  isDragging = signal(false);
  files = signal<UploadedFile[]>([]);
  error = signal('');

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(): void {
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
    const dropped = Array.from(event.dataTransfer?.files ?? []);
    this.addFiles(dropped);
  }

  onFileInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selected = Array.from(input.files ?? []);
    this.addFiles(selected);
    input.value = '';
  }

  removeFile(target: UploadedFile): void {
    this.files.update((list) => list.filter((f) => f !== target));
    if (target.previewUrl) {
      URL.revokeObjectURL(target.previewUrl);
    }
    this.filesChange.emit(this.files().map((f) => f.file));
  }

  private addFiles(newFiles: File[]): void {
    this.error.set('');
    const maxBytes = this.maxSizeMb * 1024 * 1024;
    const valid: UploadedFile[] = [];

    for (const file of newFiles) {
      if (file.size > maxBytes) {
        this.error.set(`"${file.name}" exceeds ${this.maxSizeMb} MB limit.`);
        continue;
      }
      const previewUrl = file.type.startsWith('image/') ? URL.createObjectURL(file) : null;
      valid.push({ file, previewUrl });
    }

    this.files.update((list) => (this.multiple ? [...list, ...valid] : valid));
    this.filesChange.emit(this.files().map((f) => f.file));
  }
}
