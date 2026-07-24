import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { EmployeeService } from '../../core/services/employee.service';
import { LeaveService } from '../../core/services/leave.service';

interface DashboardStats { totalEmployees: number; pendingLeaveRequests: number; onLeaveToday: number; openApprovals: number; }

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, CardModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  readonly stats = signal<DashboardStats>({ totalEmployees: 0, pendingLeaveRequests: 0, onLeaveToday: 0, openApprovals: 0 });
  readonly loading = signal(true);

  constructor(
    public authService: AuthService,
    private employeeService: EmployeeService,
    private leaveService: LeaveService,
  ) {}

  ngOnInit(): void {
    const companyId = this.authService.currentCompanyId();
    const approverId = this.authService.currentEmployeeId();
    if (!approverId || !companyId) { this.loading.set(false); return; }

    forkJoin({
      employees: this.employeeService.getByCompany(companyId),
      pending: this.leaveService.getPendingForApprover(approverId),
    }).subscribe({
      next: ({ employees, pending }) => {
        const today = new Date().toISOString().slice(0, 10);
        const onLeaveToday = employees.length
          ? pending.filter(p => p.status === 'Approved' && p.startDate <= today && p.endDate >= today).length
          : 0;
        this.stats.set({
          totalEmployees: employees.length,
          pendingLeaveRequests: pending.filter(p => p.status === 'Pending').length,
          onLeaveToday,
          openApprovals: pending.length,
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}