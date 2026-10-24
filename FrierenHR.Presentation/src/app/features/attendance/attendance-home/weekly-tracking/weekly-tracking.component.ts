import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { AttendanceService } from '../../../../core/services/attendance.service';
import { AuthService } from '../../../../core/services/auth.service';
import { AttendanceRefreshService } from '../../../../core/services/attendance-refresh.service';
import { HolidayService } from '../../../../core/services/scheduling.service';
import { AttendanceLogDto, LogWeekDayDto } from '../../../../core/models/attendance.model';
import { startOfWeek, toIsoDate, toLocalDateTime } from '../../../../core/utils/dates';

/** One row of the grid. Times are held as "HH:mm" for the inputs, not as Dates. */
interface DayRow {
  date: string;
  label: string;
  weekday: string;
  start: string;
  end: string;
  breakMinutes: number;
  /** A rest day or holiday, so the row is offered but not expected to be filled. */
  nonWorking: boolean;
  nonWorkingReason: string | null;
  /** Already recorded by the punch clock, so this row must not overwrite it. */
  punchLogged: boolean;
  enabled: boolean;
}

@Component({
  selector: 'app-weekly-tracking',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ButtonModule, InputNumberModule, InputTextModule, MessageModule,
  ],
  templateUrl: './weekly-tracking.component.html',
  styleUrl: './weekly-tracking.component.scss',
})
export class WeeklyTrackingComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly attendance = inject(AttendanceService);
  private readonly auth = inject(AuthService);
  private readonly refresh = inject(AttendanceRefreshService);
  private readonly holidays = inject(HolidayService);

  private static readonly DefaultStart = '09:00';
  private static readonly DefaultEnd = '18:00';
  private static readonly DefaultBreak = 60;

  readonly rows = signal<DayRow[]>([]);
  readonly weekStart = signal(startOfWeek(new Date()));
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  readonly weekLabel = computed(() => {
    const start = new Date(this.weekStart());
    const end = new Date(start);
    end.setDate(end.getDate() + 6);
    const sameMonth = start.getMonth() === end.getMonth();
    const startText = start.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
    const endText = end.toLocaleDateString(undefined, {
      month: sameMonth ? undefined : 'short', day: 'numeric', year: 'numeric',
    });
    return `${startText} – ${endText}`;
  });

  readonly weekHours = computed(() =>
    this.round(this.rows().reduce((total, row) => total + this.hoursFor(row), 0)));

  readonly enabledCount = computed(() => this.rows().filter(r => r.enabled).length);

  /** The first filled working day, which "copy across" uses as its template. */
  readonly templateRow = computed(() =>
    this.rows().find(r => r.enabled && !r.nonWorking && !r.punchLogged) ?? null);

  ngOnInit(): void {
    this.load();

    // Another widget on the page saving a shift changes what this grid should show.
    this.refresh.changes$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load());
  }

  load(): void {
    const employeeId = this.auth.currentEmployeeId();
    if (!employeeId) { this.loading.set(false); return; }

    const start = new Date(this.weekStart());
    const end = new Date(start);
    end.setDate(end.getDate() + 6);

    this.loading.set(true);
    this.error.set(null);

    // Local, not UTC.
    this.attendance.getByEmployee(employeeId, toLocalDateTime(start), toLocalDateTime(end)).subscribe({
      next: logs => {
        this.rows.set(this.buildRows(start, logs));
        this.loading.set(false);
        this.markNonWorkingDays(start, end);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load this week.');
      },
    });
  }

  /** Rest days and holidays are a separate, optional call. */
  private markNonWorkingDays(start: Date, end: Date): void {
    this.holidays.countWorkingDays(toIsoDate(start), toIsoDate(end)).subscribe({
      next: result => {
        const nonWorking = new Map(
          (result.nonWorkingDays ?? []).map(d => [d.date, d.reason]));

        this.rows.update(rows => rows.map(row => {
          const reason = nonWorking.get(row.date);
          return reason
            // Left enabled only if something is already logged: working a holiday happens.
            ? { ...row, nonWorking: true, nonWorkingReason: reason }
            : row;
        }));
      },
      error: () => { /* The grid works without it; nothing to tell the person. */ },
    });
  }

  private buildRows(weekStart: Date, logs: AttendanceLogDto[]): DayRow[] {
    const rows: DayRow[] = [];

    for (let offset = 0; offset < 7; offset++) {
      const date = new Date(weekStart);
      date.setDate(date.getDate() + offset);
      const iso = toIsoDate(date);

      const dayLogs = logs.filter(l => toIsoDate(new Date(l.timeIn)) === iso);
      const manual = dayLogs.find(l => l.source === 'Manual');
      const punch = dayLogs.find(l => l.source !== 'Manual');

      rows.push({
        date: iso,
        label: date.toLocaleDateString(undefined, { day: 'numeric', month: 'short' }),
        weekday: date.toLocaleDateString(undefined, { weekday: 'short' }),
        start: manual ? this.timeOf(manual.timeIn) : WeeklyTrackingComponent.DefaultStart,
        end: manual?.timeOut ? this.timeOf(manual.timeOut) : WeeklyTrackingComponent.DefaultEnd,
        breakMinutes: manual?.breakMinutes ?? WeeklyTrackingComponent.DefaultBreak,
        nonWorking: false,
        nonWorkingReason: null,
        punchLogged: !!punch,
        // Only days that already have an entry start switched on.
        enabled: !!manual,
      });
    }

    return rows;
  }

  // --- Week navigation ---

  shiftWeek(weeks: number): void {
    const next = new Date(this.weekStart());
    next.setDate(next.getDate() + weeks * 7);
    this.weekStart.set(next);
    this.notice.set(null);
    this.load();
  }

  thisWeek(): void {
    this.weekStart.set(startOfWeek(new Date()));
    this.notice.set(null);
    this.load();
  }

  // --- Row editing ---

  toggle(row: DayRow): void {
    this.update(row, { enabled: !row.enabled });
  }

  setStart(row: DayRow, value: string): void { this.update(row, { start: value }); }
  setEnd(row: DayRow, value: string): void { this.update(row, { end: value }); }
  setBreak(row: DayRow, value: number): void { this.update(row, { breakMinutes: value ?? 0 }); }

  private update(row: DayRow, changes: Partial<DayRow>): void {
    this.rows.update(rows => rows.map(r => (r.date === row.date ? { ...r, ...changes } : r)));
  }

  /** Copies the first filled day onto every other working day. */
  copyAcross(): void {
    const template = this.templateRow();
    if (!template) { this.error.set('Fill in one day first, then copy it across.'); return; }

    this.error.set(null);
    this.rows.update(rows => rows.map(row =>
      row.date === template.date || row.nonWorking || row.punchLogged
        ? row
        : {
            ...row,
            start: template.start,
            end: template.end,
            breakMinutes: template.breakMinutes,
            enabled: true,
          }));

    this.notice.set('Copied to the rest of the working week. Nothing is saved until you save.');
  }

  clearWeek(): void {
    this.rows.update(rows => rows.map(row => ({ ...row, enabled: false })));
    this.notice.set('Cleared. Saving now removes nothing already recorded; delete those from the history below.');
  }

  // --- Totals ---

  hoursFor(row: DayRow): number {
    if (!row.enabled) return 0;

    const start = this.minutesOf(row.start);
    const end = this.minutesOf(row.end);
    if (start === null || end === null) return 0;

    let span = end - start;
    if (span <= 0) span += 24 * 60; // overnight

    return Math.max(0, (span - (row.breakMinutes ?? 0)) / 60);
  }

  hoursLabel(row: DayRow): string {
    const hours = this.hoursFor(row);
    return hours > 0 ? `${this.round(hours)}h` : '—';
  }

  /** A day that cannot be saved, with the reason, so Save is never a surprise. */
  problemWith(row: DayRow): string | null {
    if (!row.enabled) return null;
    if (row.punchLogged) return 'Already clocked through the punch clock.';
    if (this.minutesOf(row.start) === null || this.minutesOf(row.end) === null) return 'Enter a valid time.';
    if (this.hoursFor(row) <= 0) return 'The break is longer than the shift.';
    return null;
  }

  readonly firstProblem = computed(() =>
    this.rows().map(row => this.problemWith(row)).find(problem => problem !== null) ?? null);

  readonly canSave = computed(() =>
    this.enabledCount() > 0 && !this.firstProblem() && !this.saving());

  // --- Saving ---

  save(): void {
    const employeeId = this.auth.currentEmployeeId();
    if (!employeeId || !this.canSave()) return;

    const days: LogWeekDayDto[] = this.rows()
      .filter(row => row.enabled && !row.punchLogged)
      .map(row => ({
        // Sent as a plain date.
        date: `${row.date}T00:00:00`,
        startTime: `${row.start}:00`,
        endTime: `${row.end}:00`,
        breakMinutes: row.breakMinutes ?? 0,
      }));

    this.saving.set(true);
    this.error.set(null);
    this.notice.set(null);

    this.attendance.logWeek({ employeeId, days }).subscribe({
      next: () => {
        this.saving.set(false);
        this.notice.set(`Saved ${days.length} day(s).`);
        this.refresh.notifyChanged();
      },
      error: err => {
        this.saving.set(false);
        // The server rejects the whole week on one bad day, so its message names the day.
        this.error.set(err?.error?.message ?? 'Could not save this week.');
      },
    });
  }

  // --- Helpers ---

  private timeOf(iso: string): string {
    const date = new Date(iso);
    return `${`${date.getHours()}`.padStart(2, '0')}:${`${date.getMinutes()}`.padStart(2, '0')}`;
  }

  private minutesOf(value: string): number | null {
    const match = /^(\d{1,2}):(\d{2})$/.exec(value ?? '');
    if (!match) return null;

    const hours = Number(match[1]);
    const minutes = Number(match[2]);
    if (hours > 23 || minutes > 59) return null;

    return hours * 60 + minutes;
  }

  private round(value: number): number {
    return Math.round(value * 100) / 100;
  }
}
