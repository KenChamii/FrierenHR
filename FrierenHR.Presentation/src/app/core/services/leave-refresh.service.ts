import { Injectable } from '@angular/core';
import { RefreshNotifier } from './refresh-notifier';

/**
 * The same thing for the Leave page: lets the request form tell the "My requests" tab to
 * reload after a submission, since they are sibling tabs with no reference to each other.
 */
@Injectable({ providedIn: 'root' })
export class LeaveRefreshService extends RefreshNotifier {}
