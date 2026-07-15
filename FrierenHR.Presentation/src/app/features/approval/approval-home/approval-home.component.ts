import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TabsModule } from 'primeng/tabs';
import { ChainConfigComponent } from './chain-config/chain-config.component';
import { PendingApprovalsInboxComponent } from './pending-approvals-inbox/pending-approvals-inbox.component';

@Component({
  selector: 'app-approval-home',
  standalone: true,
  imports: [CommonModule, TabsModule, ChainConfigComponent, PendingApprovalsInboxComponent],
  templateUrl: './approval-home.component.html',
})
export class ApprovalHomeComponent {}