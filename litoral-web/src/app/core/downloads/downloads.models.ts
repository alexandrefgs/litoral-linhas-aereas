export type DownloadCategory = 'Liveries' | 'Documents' | 'Software' | 'Other';

export const DOWNLOAD_CATEGORIES: { value: DownloadCategory; label: string; icon: string }[] = [
  { value: 'Liveries', label: 'Liveries', icon: 'ti-brush' },
  { value: 'Documents', label: 'Documents', icon: 'ti-file-text' },
  { value: 'Software', label: 'Software', icon: 'ti-device-desktop' },
  { value: 'Other', label: 'Other', icon: 'ti-package' }
];

export interface DownloadItem {
  id: string;
  title: string;
  description: string | null;
  category: DownloadCategory;
  aircraftType: string | null;
  version: string | null;
  isFile: boolean;
  fileName: string | null;
  sizeBytes: number;
  externalUrl: string | null;
  downloads: number;
  updatedAt: string;
}

export interface AdminDownloadItem extends DownloadItem {
  updatedBy: string;
}

export interface DownloadPayload {
  title: string;
  description: string | null;
  category: DownloadCategory;
  aircraftType: string | null;
  version: string | null;
  externalUrl: string | null;
  file: File | null;
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}