import { Injectable } from '@angular/core';
import { RefreshNotifier } from './refresh-notifier';

/** "Something changed" for the Attendance page. */
@Injectable({ providedIn: 'root' })
export class AttendanceRefreshService extends RefreshNotifier {}
