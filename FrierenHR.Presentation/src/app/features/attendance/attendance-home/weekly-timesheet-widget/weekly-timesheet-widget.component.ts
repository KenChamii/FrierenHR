import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
import { endOfWeek, startOfWeek, toLocalDateTime } from '../../../../core/utils/dates';

@Component({
  selector: 'app-weekly-timesheet-widget',
  standalone: true,
  imports: [CommonModule, CardModule, ButtonModule, TagModule, MessageModule],
  templateUrl: './weekly-timesheet-widget.component.html',
  styleUrl: './weekly-timesheet-widget.component.scss',
})
export class WeeklyTimesheetWidgetComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly timesheet = signal<TimesheetDto | null>(null);
  readonly hoursSoFar = signal(0);
  readonly errorMessage = signal<string | null>(null);

  readonly weekStart = startOfWeek(new Date());
  readonly weekEnd = endOfWeek(new Date());

  readonly totalHours = computed(() => this.timesheet()?.totalHours ?? this.hoursSoFar());
  readonly status = computed<TimesheetStatus>(() => this.timesheet()?.status ?? 'Draft');
  readonly canSubmit = computed(() => this.status() === 'Draft' || this.status() === 'Rejected');

  /**
   * An employee may recall their own submission while it is still awaiting a decision.
   * Reopening an approved week is HR's, since somebody has already acted on it, and the
   * server enforces that regardless of what this shows.
   */
  readonly canReopen = computed(() =>
    this.status() === 'Submitted'
    || (this.status() === 'Approved' && this.authService.currentRole() === 'HRAdmin'));

  readonly reopening = signal(false);

  constructor(
    private timesheetService: TimesheetService,
    private attendanceService: AttendanceService,
    public authService: AuthService,
    private refreshService: AttendanceRefreshService,
  ) {}

  ngOnInit(): void {
    this.load();

    // Keeps "hours so far" live when a clock-in or shift widget on the page saves.
    this.refreshService.changes$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load());
  }

  load(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) { this.loading.set(false); return; }
    this.loading.set(true);

    // Sent as a local date with no zone suffix. toISOString() turned local Monday midnight
    // into the previous Sunday in UTC+8, and the server's week-start rule then moved that
    // back another six days, so this read an entirely different week.
    this.timesheetService.getForWeek(employeeId, toLocalDateTime(this.weekStart)).subscribe({
      next: (ts) => {
        this.timesheet.set(ts);
        if (ts) { this.loading.set(false); return; }
        // Nothing submitted yet this week — total so far comes straight from attendance logs.
        this.attendanceService.getByEmployee(
          employeeId, toLocalDateTime(this.weekStart), toLocalDateTime(this.weekEnd)).subscribe({
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
    this.timesheetService.submit({ employeeId, weekStartDate: toLocalDateTime(this.weekStart) }).subscribe({
      next: (ts) => { this.timesheet.set(ts); this.submitting.set(false); this.refreshService.notifyChanged(); },
      error: (err) => { this.submitting.set(false); this.errorMessage.set(err?.error?.message ?? 'Could not submit this week.'); },
    });
  }

  reopen(): void {
    const id = this.timesheet()?.id;
    if (!id) return;

    this.reopening.set(true);
    this.errorMessage.set(null);
    this.timesheetService.reopen(id).subscribe({
      next: ts => {
        this.timesheet.set(ts);
        this.reopening.set(false);
        // The week has just unlocked, so the grid and history beside it can accept edits.
        this.refreshService.notifyChanged();
      },
      error: err => {
        this.reopening.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Could not reopen this week.');
      },
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

}
