import { HttpErrorResponse } from '@angular/common/http';

export function apiErrorStatus(error: unknown): number {
  return error instanceof HttpErrorResponse ? error.status : 0;
}

export function apiErrorMessage(error: unknown): string | null {
  if (!(error instanceof HttpErrorResponse) || error.status < 400 || error.status >= 500) return null;

  const title = error.error?.title;
  return typeof title === 'string' && title.length > 0 ? title : null;
}