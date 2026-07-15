import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { AttendanceService } from '../../../../core/services/attendance.service';
import { AuthService } from '../../../../core/services/auth.service';
import { AttendanceLogDto } from '../../../../core/models/attendance.model';

@Component({
  selector: 'app-clock-in-widget',
  standalone: true,
  imports: [CommonModule, CardModule, ButtonModule],
  templateUrl: './clock-in-widget.component.html',
  styleUrl: './clock-in-widget.component.scss',
})
export class ClockInWidgetComponent implements OnInit {
  readonly openLog = signal<AttendanceLogDto | null>(null);
  readonly loading = signal(true);
  readonly working = signal(false);

  constructor(private attendanceService: AttendanceService, private authService: AuthService) {}

  ngOnInit(): void { this.refresh(); }

  refresh(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) { this.loading.set(false); return; }
    const today = new Date().toISOString().slice(0, 10);
    this.loading.set(true);
    this.attendanceService.getByEmployee(employeeId, today).subscribe({
      next: (logs) => { this.openLog.set(logs.find(l => !l.timeOut) ?? null); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  clockIn(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) return;
    this.working.set(true);
    this.attendanceService.clockIn({ employeeId, source: 'Web' }).subscribe({
      next: () => { this.working.set(false); this.refresh(); },
      error: () => this.working.set(false),
    });
  }

  clockOut(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) return;
    this.working.set(true);
    this.attendanceService.clockOut({ employeeId }).subscribe({
      next: () => { this.working.set(false); this.refresh(); },
      error: () => this.working.set(false),
    });
  }
}