import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ClockInWidgetComponent } from './clock-in-widget/clock-in-widget.component';
import { ShiftClockWidgetComponent } from './shift-clock-widget/shift-clock-widget.component';
import { AttendanceHistoryComponent } from './attendance-history/attendance-history.component';
import { WeeklyTimesheetWidgetComponent } from './weekly-timesheet-widget/weekly-timesheet-widget.component';
import { PendingTimesheetsWidgetComponent } from './pending-timesheets-widget/pending-timesheets-widget.component';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-attendance-home',
  standalone: true,
  imports: [
    CommonModule,
    ClockInWidgetComponent,
    ShiftClockWidgetComponent,
    AttendanceHistoryComponent,
    WeeklyTimesheetWidgetComponent,
    PendingTimesheetsWidgetComponent,
  ],
  templateUrl: './attendance-home.component.html',
  styleUrl: './attendance-home.component.scss',
})
export class AttendanceHomeComponent {
  constructor(public authService: AuthService) {}
}
