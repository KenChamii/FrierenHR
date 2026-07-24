import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { TimesheetService } from '../../../../core/services/timesheet.service';
import { AuthService } from '../../../../core/services/auth.service';
import { TimesheetDto } from '../../../../core/models/timesheet.model';

@Component({
  selector: 'app-pending-timesheets-widget',
  standalone: true,
  imports: [CommonModule, CardModule, ButtonModule, MessageModule],
  templateUrl: './pending-timesheets-widget.component.html',
  styleUrl: './pending-timesheets-widget.component.scss',
})
export class PendingTimesheetsWidgetComponent implements OnInit {
  readonly loading = signal(true);
  readonly items = signal<TimesheetDto[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly decidingId = signal<string | null>(null);

  constructor(private timesheetService: TimesheetService, public authService: AuthService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    const approverId = this.authService.currentEmployeeId();
    if (!approverId) { this.loading.set(false); return; }
    this.loading.set(true);
    this.timesheetService.getPendingForApprover(approverId).subscribe({
      next: (list) => { this.items.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); this.errorMessage.set('Could not load pending timesheets.'); },
    });
  }

  decide(item: TimesheetDto, approve: boolean): void {
    const approverId = this.authService.currentEmployeeId();
    if (!approverId) return;
    const comment = approve ? undefined : (window.prompt('Reason for rejecting (visible to the employee):') ?? undefined);
    if (!approve && comment === undefined) return; // cancelled the prompt

    this.decidingId.set(item.id);
    this.errorMessage.set(null);
    this.timesheetService.decide(item.id, { decidedByEmployeeId: approverId, approve, comment }).subscribe({
      next: () => { this.decidingId.set(null); this.load(); },
      error: (err) => { this.decidingId.set(null); this.errorMessage.set(err?.error?.message ?? 'Could not record that decision.'); },
    });
  }
}
