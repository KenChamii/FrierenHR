import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { EmployeeService } from '../../core/services/employee.service';
import { AuthService } from '../../core/services/auth.service';

function matchesNewPassword(control: AbstractControl): ValidationErrors | null {
  const group = control.parent;
  if (!group) return null;
  return group.get('newPassword')?.value === control.value ? null : { mismatch: true };
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CardModule, PasswordModule, ButtonModule, MessageModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent {
  private readonly fb = inject(FormBuilder);
  private readonly employeeService = inject(EmployeeService);
  readonly authService = inject(AuthService);

  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Mirrors the backend PasswordPolicy (min 8 chars, at least one letter and one digit).
  form = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d).+$/)]],
    confirmPassword: ['', [Validators.required, matchesNewPassword]],
  });

  submit(): void {
    if (this.form.invalid) return;
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) return;

    this.saving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const raw = this.form.getRawValue();
    this.employeeService.changePassword(employeeId, {
      currentPassword: raw.currentPassword!,
      newPassword: raw.newPassword!,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMessage.set('Password updated.');
        this.form.reset();
      },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err?.error?.message ?? 'Could not update password.'); },
    });
  }
}
