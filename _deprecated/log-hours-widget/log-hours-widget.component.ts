import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { DatePickerModule } from 'primeng/datepicker';
import { AttendanceService } from '../../../../core/services/attendance.service';
import { AuthService } from '../../../../core/services/auth.service';
import { AttendanceLogDto } from '../../../../core/models/attendance.model';

// Remote-friendly alternative to the punch clock: employee enters how many
// hours they worked for a given day instead of clocking in/out. Backed by
// the existing /api/attendance/log-hours endpoint (Source = "Manual").
@Component({
  selector: 'app-log-hours-widget',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CardModule, ButtonModule, InputNumberModule, DatePickerModule],
  templateUrl: './log-hours-widget.component.html',
  styleUrl: './log-hours-widget.component.scss',
})
export class LogHoursWidgetComponent implements OnInit {
  private readonly fb = new FormBuilder();

  readonly todayLog = signal<AttendanceLogDto | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  form = this.fb.group({
    date: [new Date(), Validators.required],
    hours: [8, [Validators.required, Validators.min(0.5), Validators.max(24)]],
  });

  constructor(private attendanceService: AttendanceService, private authService: AuthService) {}

  ngOnInit(): void { this.refresh(); }

  refresh(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) { this.loading.set(false); return; }
    const today = new Date().toISOString().slice(0, 10);
    this.loading.set(true);
    this.attendanceService.getByEmployee(employeeId, today).subscribe({
      next: (logs) => {
        const manual = logs.find(l => l.source === 'Manual') ?? null;
        this.todayLog.set(manual);
        if (manual?.hoursWorked) this.form.patchValue({ hours: manual.hoursWorked });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  submit(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId || this.form.invalid) return;
    const raw = this.form.getRawValue();
    this.saving.set(true);
    this.errorMessage.set(null);
    this.attendanceService.logHours({
      employeeId,
      date: (raw.date as Date).toISOString(),
      hours: raw.hours as number,
    }).subscribe({
      next: () => { this.saving.set(false); this.refresh(); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err?.error?.message ?? 'Could not save hours.'); },
    });
  }
}
