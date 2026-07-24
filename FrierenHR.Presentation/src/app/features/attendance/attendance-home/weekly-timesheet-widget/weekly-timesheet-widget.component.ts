import { Component, OnInit, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { MessageModule } from 'primeng/message';
import { TimesheetService } from '../../../../core/services/timesheet.service';
import { AttendanceService } from '../../../../core/services/attendance.service';
import { AuthService } from '../../../../core/services/auth.service';
import { AttendanceRefreshService } from '../../../../core/services/attendance-refresh.service';
import { TimesheetDto, TimesheetStatus } from '../../../../core/models/timesheet.model';

@Component({
  selector: 'app-weekly-timesheet-widget',
  standalone: true,
  imports: [CommonModule, CardModule, ButtonModule, TagModule, MessageModule],
  templateUrl: './weekly-timesheet-widget.component.html',
  styleUrl: './weekly-timesheet-widget.component.scss',
})
export class WeeklyTimesheetWidgetComponent implements OnInit {
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly timesheet = signal<TimesheetDto | null>(null);
  readonly hoursSoFar = signal(0);
  readonly errorMessage = signal<string | null>(null);

  readonly weekStart = this.getWeekStart(new Date());
  readonly weekEnd = new Date(this.weekStart.getTime() + 6 * 24 * 60 * 60 * 1000);

  readonly totalHours = computed(() => this.timesheet()?.totalHours ?? this.hoursSoFar());
  readonly status = computed<TimesheetStatus>(() => this.timesheet()?.status ?? 'Draft');
  readonly canSubmit = computed(() => this.status() === 'Draft' || this.status() === 'Rejected');

  constructor(
    private timesheetService: TimesheetService,
    private attendanceService: AttendanceService,
    public authService: AuthService,
    private refreshService: AttendanceRefreshService,
  ) {
    // Keeps "hours so far" live when a clock-in/shift-log widget elsewhere on the page saves.
    effect(() => {
      this.refreshService.version();
      this.load();
    });
  }

  ngOnInit(): void {}

  load(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) { this.loading.set(false); return; }
    this.loading.set(true);

    this.timesheetService.getForWeek(employeeId, this.weekStart.toISOString()).subscribe({
      next: (ts) => {
        this.timesheet.set(ts);
        if (ts) { this.loading.set(false); return; }
        // Nothing submitted yet this week — total so far comes straight from attendance logs.
        this.attendanceService.getByEmployee(employeeId, this.weekStart.toISOString(), this.weekEnd.toISOString()).subscribe({
          next: (logs) => {
            this.hoursSoFar.set(logs.reduce((sum, l) => sum + (l.hoursWorked ?? 0), 0));
            this.loading.set(false);
          },
          error: () => this.loading.set(false),
        });
      },
      error: () => this.loading.set(false),
    });
  }

  submit(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) return;
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.timesheetService.submit({ employeeId, weekStartDate: this.weekStart.toISOString() }).subscribe({
      next: (ts) => { this.timesheet.set(ts); this.submitting.set(false); this.refreshService.notifyChanged(); },
      error: (err) => { this.submitting.set(false); this.errorMessage.set(err?.error?.message ?? 'Could not submit this week.'); },
    });
  }

  severityFor(status: TimesheetStatus): 'secondary' | 'info' | 'success' | 'danger' {
    switch (status) {
      case 'Submitted': return 'info';
      case 'Approved': return 'success';
      case 'Rejected': return 'danger';
      default: return 'secondary';
    }
  }

  private getWeekStart(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay(); // 0 = Sunday
    const diff = day === 0 ? -6 : 1 - day; // shift back to Monday
    d.setDate(d.getDate() + diff);
    d.setHours(0, 0, 0, 0);
    return d;
  }
}
