import { Component, OnInit, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { AttendanceService } from '../../../../core/services/attendance.service';
import { AuthService } from '../../../../core/services/auth.service';
import { AttendanceRefreshService } from '../../../../core/services/attendance-refresh.service';
import { AttendanceLogDto } from '../../../../core/models/attendance.model';

@Component({
  selector: 'app-attendance-history',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, MessageModule, ConfirmDialogModule],
  providers: [ConfirmationService],
  templateUrl: './attendance-history.component.html',
})
export class AttendanceHistoryComponent implements OnInit {
  readonly logs = signal<AttendanceLogDto[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  constructor(
    private attendanceService: AttendanceService,
    private authService: AuthService,
    private confirmationService: ConfirmationService,
    private refreshService: AttendanceRefreshService,
  ) {
    // Reruns whenever any widget on the page calls notifyChanged() — including once
    // immediately on creation, so this replaces the old ngOnInit() load call too.
    effect(() => {
      this.refreshService.version();
      this.refresh();
    });
  }

  ngOnInit(): void {}

  refresh(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) { this.loading.set(false); return; }
    this.loading.set(true);
    this.attendanceService.getByEmployee(employeeId).subscribe({
      next: (list) => { this.logs.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); this.errorMessage.set('Could not load attendance history.'); },
    });
  }

  confirmDelete(log: AttendanceLogDto): void {
    this.confirmationService.confirm({
      header: 'Delete entry?',
      message: `Remove this attendance entry from ${new Date(log.timeIn).toLocaleDateString()}? This can't be undone.`,
      acceptButtonProps: { severity: 'danger', label: 'Delete' },
      rejectButtonProps: { severity: 'secondary', outlined: true, label: 'Cancel' },
      accept: () => this.deleteLog(log.id),
    });
  }

  private deleteLog(id: string): void {
    this.errorMessage.set(null);
    this.attendanceService.deleteLog(id).subscribe({
      next: () => this.refreshService.notifyChanged(),
      // Most common failure here is the week already being locked (submitted/approved) —
      // surface the backend's message instead of failing silently.
      error: (err) => this.errorMessage.set(err?.error?.message ?? 'Could not delete this entry.'),
    });
  }
}
