import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { AuthService } from '../../../core/services/auth.service';
import { EmployeeService } from '../../../core/services/employee.service';

/** Mirrors PasswordPolicy on the server, which remains the enforcement point. */
function passwordPolicy(control: AbstractControl): ValidationErrors | null {
  const value = (control.value ?? '') as string;
  if (value.length < 8) return { policy: 'At least 8 characters.' };
  if (!/[a-zA-Z]/.test(value)) return { policy: 'At least one letter.' };
  if (!/[0-9]/.test(value)) return { policy: 'At least one number.' };
  return null;
}

/** The new password has to differ from the temporary one, or nothing has been achieved. */
function mustDiffer(group: AbstractControl): ValidationErrors | null {
  const current = group.get('currentPassword')?.value;
  const next = group.get('newPassword')?.value;
  return current && next && current === next ? { unchanged: true } : null;
}

@Component({
  selector: 'app-set-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ButtonModule, MessageModule, PasswordModule],
  templateUrl: './set-password.component.html',
  styleUrl: './set-password.component.scss',
})
export class SetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly employees = inject(EmployeeService);
  private readonly router = inject(Router);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, passwordPolicy]],
    confirmPassword: ['', Validators.required],
  }, { validators: mustDiffer });

  get policyHint(): string | null {
    const control = this.form.controls.newPassword;
    return control.touched && control.errors?.['policy'] ? control.errors['policy'] : null;
  }

  get mismatch(): boolean {
    const { newPassword, confirmPassword } = this.form.controls;
    return confirmPassword.touched && !!confirmPassword.value
      && newPassword.value !== confirmPassword.value;
  }

  get unchanged(): boolean {
    return this.form.touched && this.form.errors?.['unchanged'] === true;
  }

  submit(): void {
    const employeeId = this.auth.currentEmployeeId();
    if (!employeeId || this.form.invalid || this.mismatch) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();

    this.saving.set(true);
    this.error.set(null);

    this.employees.changePassword(employeeId, {
      currentPassword: raw.currentPassword!,
      newPassword: raw.newPassword!,
    }).subscribe({
      next: result => {
        this.saving.set(false);
        // The old token still says a change is required.
        if (result?.token) this.auth.onPasswordChanged(result.token);
        this.router.navigateByUrl('/dashboard');
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.message ?? 'Could not change your password.');
      },
    });
  }
}
