import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { AuthService } from '../../../core/services/auth.service';
import { EmployeeService } from '../../../core/services/employee.service';
import { RegisterCompanyDto } from '../../../core/models/auth.model';

/** Mirrors PasswordPolicy on the server. Validated there too; this is for immediate feedback. */
function passwordPolicy(control: AbstractControl): ValidationErrors | null {
  const value = (control.value ?? '') as string;
  if (value.length < 8) return { policy: 'At least 8 characters.' };
  if (!/[a-zA-Z]/.test(value)) return { policy: 'At least one letter.' };
  if (!/[0-9]/.test(value)) return { policy: 'At least one number.' };
  return null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, ButtonModule, InputTextModule,
    MessageModule, PasswordModule,
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly employees = inject(EmployeeService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  form = this.fb.group({
    companyName: ['', [Validators.required, Validators.maxLength(200)]],
    // Short, so it reads well as an identifier. Upper-cased by the server.
    companyCode: ['', [Validators.required, Validators.maxLength(20)]],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, passwordPolicy]],
  });

  get passwordHint(): string | null {
    const control = this.form.controls.password;
    return control.touched && control.errors?.['policy'] ? control.errors['policy'] : null;
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.loading.set(true);
    this.errorMessage.set(null);

    this.auth.registerCompany(this.form.getRawValue() as RegisterCompanyDto).subscribe({
      next: result => {
        // Same two-step as login: establish the session so the interceptor has a token.
        this.auth.setSession(result);
        this.employees.getById(result.employeeId).subscribe({
          next: employee => {
            this.auth.updateCompanyId(employee.companyId);
            this.loading.set(false);
            this.router.navigateByUrl('/dashboard');
          },
          error: () => {
            // Not fatal.
            this.loading.set(false);
            this.router.navigateByUrl('/dashboard');
          },
        });
      },
      error: err => {
        this.loading.set(false);
        // The API returns the reason for a rejected sign-up, such as a company code already taken.
        this.errorMessage.set(err?.error?.message ?? 'Could not create the company.');
      },
    });
  }
}
