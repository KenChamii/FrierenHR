import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ClockInWidgetComponent } from './clock-in-widget/clock-in-widget.component';
import { WeeklyTrackingComponent } from './weekly-tracking/weekly-tracking.component';
import { ShiftClockWidgetComponent } from './shift-clock-widget/shift-clock-widget.component';
import { AttendanceHistoryComponent } from './attendance-history/attendance-history.component';
import { WeeklyTimesheetWidgetComponent } from './weekly-timesheet-widget/weekly-timesheet-widget.component';
import { PendingTimesheetsWidgetComponent } from './pending-timesheets-widget/pending-timesheets-widget.component';
import { AuthService } from '../../../core/services/auth.service';

type TrackingTab = 'week' | 'today';

@Component({
  selector: 'app-attendance-home',
  standalone: true,
  imports: [
    CommonModule,
    ClockInWidgetComponent,
    WeeklyTrackingComponent,
    ShiftClockWidgetComponent,
    AttendanceHistoryComponent,
    WeeklyTimesheetWidgetComponent,
    PendingTimesheetsWidgetComponent,
  ],
  templateUrl: './attendance-home.component.html',
  styleUrl: './attendance-home.component.scss',
})
export class AttendanceHomeComponent {
  /**
   * The week grid leads. Logging a fixed remote schedule a day at a time was the complaint
   * the grid exists to answer, so it should not be the tab people have to find.
   */
  readonly tab = signal<TrackingTab>('week');

  constructor(public authService: AuthService) {}

  select(tab: TrackingTab): void { this.tab.set(tab); }
}
