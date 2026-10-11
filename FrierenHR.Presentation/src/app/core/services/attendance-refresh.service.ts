import { Injectable } from '@angular/core';
import { RefreshNotifier } from './refresh-notifier';

/**
 * "Something changed" for the Attendance page. The clock-in widget, the shift clock, the
 * weekly timesheet and the history table are siblings with no reference to each other, so
 * without this a saved entry only appeared after a manual page reload.
 *
 * Call notifyChanged() after any successful create, update or delete.
 */
@Injectable({ providedIn: 'root' })
export class AttendanceRefreshService extends RefreshNotifier {}
