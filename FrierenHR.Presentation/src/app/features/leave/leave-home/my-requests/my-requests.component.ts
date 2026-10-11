import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { LeaveService } from '../../../../core/services/leave.service';
import { AuthService } from '../../../../core/services/auth.service';
import { LeaveRefreshService } from '../../../../core/services/leave-refresh.service';
import { LeaveRequestDto } from '../../../../core/models/leave.model';

@Component({
  selector: 'app-my-requests',
  standalone: true,
  imports: [CommonModule, TableModule, TagModule],
  templateUrl: './my-requests.component.html',
})
export class MyRequestsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  readonly requests = signal<LeaveRequestDto[]>([]);
  readonly loading = signal(true);

  constructor(
    private leaveService: LeaveService,
    private authService: AuthService,
    private refreshService: LeaveRefreshService,
  ) {}

  ngOnInit(): void {
    this.load();

    // Reloads whenever the request form reports a new submission.
    this.refreshService.changes$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load());
  }

  load(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) { this.loading.set(false); return; }
    this.loading.set(true);
    this.leaveService.getByEmployee(employeeId).subscribe({
      next: (list) => { this.requests.set(list); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  severityFor(status: string): 'success' | 'danger' | 'warn' | 'info' {
    return status === 'Approved' ? 'success' : status === 'Rejected' ? 'danger' : status === 'Escalated' ? 'warn' : 'info';
  }
}
