import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { LeaveService } from '../../../core/services/leave.service';
import { AuthService } from '../../../core/services/auth.service';
import { LeaveBalanceDto } from '../../../core/models/leave.model';

@Component({
  selector: 'app-leave-balance-list',
  standalone: true,
  imports: [CommonModule, TableModule],
  templateUrl: './leave-balance-list.component.html',
})
export class LeaveBalanceListComponent implements OnInit {
  readonly balances = signal<LeaveBalanceDto[]>([]);
  readonly loading = signal(true);

  constructor(private leaveService: LeaveService, private authService: AuthService) {}

  ngOnInit(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) { this.loading.set(false); return; }
    this.leaveService.getBalances(employeeId).subscribe({
      next: (list) => { this.balances.set(list); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}