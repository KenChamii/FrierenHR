import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { LeaveService } from '../../../core/services/leave.service';
import { AuthService } from '../../../core/services/auth.service';
import { LeaveRequestDto } from '../../../core/models/leave.model';

@Component({
  selector: 'app-approval-queue',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, TagModule],
  templateUrl: './approval-queue.component.html',
})
export class ApprovalQueueComponent implements OnInit {
  readonly requests = signal<LeaveRequestDto[]>([]);
  readonly loading = signal(true);
  readonly decidingId = signal<string | null>(null);

  constructor(private leaveService: LeaveService, private authService: AuthService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    const approverId = this.authService.currentEmployeeId();
    if (!approverId) { this.loading.set(false); return; }
    this.loading.set(true);
    this.leaveService.getPendingForApprover(approverId).subscribe({
      next: (list) => { this.requests.set(list); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  decide(request: LeaveRequestDto, approve: boolean): void {
    const decidedByEmployeeId = this.authService.currentEmployeeId();
    if (!decidedByEmployeeId) return;
    this.decidingId.set(request.id);
    this.leaveService.decide(request.id, { decidedByEmployeeId, approve }).subscribe({
      next: () => { this.decidingId.set(null); this.load(); },
      error: () => this.decidingId.set(null),
    });
  }

  severityFor(status: string): 'success' | 'danger' | 'warn' | 'info' {
    return status === 'Approved' ? 'success' : status === 'Rejected' ? 'danger' : status === 'Escalated' ? 'warn' : 'info';
  }
}