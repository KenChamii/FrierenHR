import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { AttendanceService } from '../../../../core/services/attendance.service';
import { AuthService } from '../../../../core/services/auth.service';
import { AttendanceLogDto } from '../../../../core/models/attendance.model';

@Component({
  selector: 'app-attendance-history',
  standalone: true,
  imports: [CommonModule, TableModule],
  templateUrl: './attendance-history.component.html',
})
export class AttendanceHistoryComponent implements OnInit {
  readonly logs = signal<AttendanceLogDto[]>([]);
  readonly loading = signal(true);

  constructor(private attendanceService: AttendanceService, private authService: AuthService) {}

  ngOnInit(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) { this.loading.set(false); return; }
    this.attendanceService.getByEmployee(employeeId).subscribe({
      next: (list) => { this.logs.set(list); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}