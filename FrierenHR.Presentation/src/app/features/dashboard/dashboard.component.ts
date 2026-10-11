import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { AuthService } from '../../core/services/auth.service';
import { EmployeeService } from '../../core/services/employee.service';
import { LeaveService } from '../../core/services/leave.service';
import { ReportingService } from '../../core/services/reporting.service';
import { toIsoDate } from '../../core/utils/dates';

interface DashboardStats {
  totalEmployees: number;
  awaitingMyDecision: number;
  onLeaveToday: number;
  myLeaveBalance: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, CardModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly employees = inject(EmployeeService);
  private readonly leave = inject(LeaveService);
  private readonly reports = inject(ReportingService);

  readonly stats = signal<DashboardStats>({
    totalEmployees: 0, awaitingMyDecision: 0, onLeaveToday: 0, myLeaveBalance: 0,
  });
  readonly loading = signal(true);

  /** Both company-wide figures need a role that is allowed to see other people's data. */
  readonly canSeeCompanyFigures = computed(() =>
    this.auth.currentRole() === 'Manager' || this.auth.currentRole() === 'HRAdmin');

  // Kept public for the template's greeting.
  get authService(): AuthService { return this.auth; }

  ngOnInit(): void {
    const companyId = this.auth.currentCompanyId();
    const employeeId = this.auth.currentEmployeeId();
    if (!employeeId || !companyId) { this.loading.set(false); return; }

    const today = toIsoDate(new Date());

    forkJoin({
      // Each call is allowed to fail on its own. One tile that cannot load should not blank
      // the whole dashboard, which is what a bare forkJoin would do.
      headcount: this.guard(this.employees.getByCompany(companyId).pipe(map(list => list.length)), 0),
      awaiting: this.guard(this.leave.getPendingForApprover(employeeId).pipe(map(list => list.length)), 0),
      balance: this.guard(
        this.leave.getBalances(employeeId).pipe(
          map(list => list.reduce((total, b) => total + b.balance, 0))), 0),
      // Counted from the availability report rather than from the approver's pending queue.
      // The old code filtered that queue for status 'Approved', and since the queue only ever
      // contains pending requests, the answer was always zero.
      onLeave: this.canSeeCompanyFigures()
        ? this.guard(
            this.reports.availability(today, today).pipe(
              map(data => data.rows.filter(row =>
                row.days.some(day => day.status === 'OnLeave')).length)), 0)
        : of(0),
    }).subscribe({
      next: ({ headcount, awaiting, balance, onLeave }) => {
        this.stats.set({
          totalEmployees: headcount,
          awaitingMyDecision: awaiting,
          onLeaveToday: onLeave,
          myLeaveBalance: Math.round(balance * 100) / 100,
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private guard<T>(source: Observable<T>, fallback: T): Observable<T> {
    return source.pipe(catchError(() => of(fallback)));
  }
}
