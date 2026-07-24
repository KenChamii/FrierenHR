import { Injectable, signal } from '@angular/core';

/**
 * Lightweight cross-component "something changed" signal for the Attendance page.
 * The clock-in widget, shift-clock widget, and attendance history table are sibling
 * components — none of them know about each other directly, so without this, saving
 * or deleting an entry in one widget had no way to tell the others to refresh (you'd
 * only see the change after a manual page reload).
 *
 * Usage: call notifyChanged() after any successful create/update/delete. Anything that
 * displays attendance/timesheet data reacts to version() via an effect() and reloads.
 */
@Injectable({ providedIn: 'root' })
export class AttendanceRefreshService {
  readonly version = signal(0);
  notifyChanged(): void {
    this.version.update((v) => v + 1);
  }
}
