import { HttpErrorResponse } from '@angular/common/http';

export function apiErrorStatus(error: unknown): number {
  return error instanceof HttpErrorResponse ? error.status : 0;
}