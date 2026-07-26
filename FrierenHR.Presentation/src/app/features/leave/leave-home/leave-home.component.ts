import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TabsModule } from 'primeng/tabs';
import { LeaveRequestFormComponent } from './leave-request-form/leave-request-form.component';
import { LeaveBalanceListComponent } from './leave-balance-list/leave-balance-list.component';
import { ApprovalQueueComponent } from './approval-queue/approval-queue.component';
import { MyRequestsComponent } from './my-requests/my-requests.component';

@Component({
  selector: 'app-leave-home',
  standalone: true,
  imports: [CommonModule, TabsModule, LeaveRequestFormComponent, LeaveBalanceListComponent, ApprovalQueueComponent, MyRequestsComponent],
  templateUrl: './leave-home.component.html',
})
export class LeaveHomeComponent {}
