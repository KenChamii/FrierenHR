import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { CheckboxModule } from 'primeng/checkbox';
import { AuthService } from '../../../core/services/auth.service';
import { EmployeeService } from '../../../core/services/employee.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputTextModule, PasswordModule, ButtonModule, MessageModule, CheckboxModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly employeeService = inject(EmployeeService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    remember: [false],
  });

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.errorMessage.set(null);
    this.authService.login(this.form.getRawValue() as { email: string; password: string }).subscribe({
      next: (result) => {
        // Establish the session first so the auth interceptor has a token
        // to attach, then resolve companyId from the employee's own record
        // — the login response doesn't carry it, and downstream screens
        // (Employees, Dashboard) need it to scope their API calls.
        this.authService.setSession(result);
        this.employeeService.getById(result.employeeId).subscribe({
          next: (employee) => {
            this.authService.updateCompanyId(employee.companyId);
            this.loading.set(false);
            this.router.navigateByUrl('/dashboard');
          },
          error: () => {
            // Non-fatal: proceed logged-in even if this lookup fails; the
            // affected screens will just show empty state until refreshed.
            this.loading.set(false);
            this.router.navigateByUrl('/dashboard');
          },
        });
      },
      error: () => { this.loading.set(false); this.errorMessage.set('Invalid email or password.'); },
    });
  }
}
