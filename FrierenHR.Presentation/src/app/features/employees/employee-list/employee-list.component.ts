import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { EmployeeService } from '../../../core/services/employee.service';
import { AuthService } from '../../../core/services/auth.service';
import { EmployeeDto, ResetPasswordResultDto } from '../../../core/models/employee.model';
import { EmployeeFormComponent } from './employee-form/employee-form.component';
import { MessageModule } from 'primeng/message';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [
    CommonModule, TableModule, ButtonModule, TagModule, DialogModule, MessageModule,
    TooltipModule, EmployeeFormComponent,
  ],
  templateUrl: './employee-list.component.html',
  styleUrl: './employee-list.component.scss',
})
export class EmployeeListComponent implements OnInit {
  readonly employees = signal<EmployeeDto[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly editingEmployee = signal<EmployeeDto | null>(null);

  /** Held only until the dialog closes. The password cannot be fetched again. */
  readonly issuedPassword = signal<ResetPasswordResultDto | null>(null);
  readonly resettingId = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  constructor(private employeeService: EmployeeService, public authService: AuthService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    const companyId = this.authService.currentCompanyId();
    if (!companyId) { this.loading.set(false); return; }
    this.loading.set(true);
    this.employeeService.getByCompany(companyId).subscribe({
      next: (list) => { this.employees.set(list); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  openCreate(): void { this.editingEmployee.set(null); this.dialogVisible.set(true); }
  openEdit(employee: EmployeeDto): void { this.editingEmployee.set(employee); this.dialogVisible.set(true); }

  onSaved(): void { this.dialogVisible.set(false); this.load(); }

  /**
   * Issues a new temporary password for somebody who cannot sign in. There is no email, so
   * it is shown to HR once and passed on by hand.
   */
  resetPassword(employee: EmployeeDto): void {
    this.error.set(null);
    this.resettingId.set(employee.id);

    this.employeeService.resetPassword(employee.id).subscribe({
      next: result => {
        this.resettingId.set(null);
        this.issuedPassword.set(result);
        this.load();
      },
      error: err => {
        this.resettingId.set(null);
        this.error.set(err?.error?.message ?? 'Could not reset that password.');
      },
    });
  }

  dismissIssuedPassword(): void { this.issuedPassword.set(null); }

  async copyIssuedPassword(): Promise<void> {
    const issued = this.issuedPassword();
    if (!issued) return;

    try {
      await navigator.clipboard.writeText(issued.temporaryPassword);
    } catch {
      // Clipboard access can be refused. The password is on screen either way, so there is
      // nothing to recover from and nothing worth interrupting anyone about.
    }
  }

  severityFor(role: string): 'success' | 'info' | 'warn' {
    return role === 'HRAdmin' ? 'warn' : role === 'Manager' ? 'info' : 'success';
  }
}