import { Injectable, signal } from '@angular/core';

/** Same pattern as AttendanceRefreshService: lets the leave request form tell the
 * "My Requests" history tab to reload after a successful submission, since they're
 * sibling tab components with no direct reference to each other. */
@Injectable({ providedIn: 'root' })
export class LeaveRefreshService {
  readonly version = signal(0);
  notifyChanged(): void {
    this.version.update((v) => v + 1);
  }
}
