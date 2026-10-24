import { Injectable } from '@angular/core';
import { RefreshNotifier } from './refresh-notifier';

/** The same thing for the Leave page: lets the request form tell the "My requests" tab to reload after a submission. */
@Injectable({ providedIn: 'root' })
export class LeaveRefreshService extends RefreshNotifier {}
