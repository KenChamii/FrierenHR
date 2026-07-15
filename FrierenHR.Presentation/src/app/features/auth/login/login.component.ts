import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputTextModule, PasswordModule, ButtonModule, MessageModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  constructor(private fb: FormBuilder, private authService: AuthService, private router: Router) {}

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.errorMessage.set(null);
    this.authService.login(this.form.getRawValue() as { email: string; password: string }).subscribe({
      next: (result) => { this.authService.setSession(result); this.loading.set(false); this.router.navigateByUrl('/dashboard'); },
      error: () => { this.loading.set(false); this.errorMessage.set('Invalid email or password.'); },
    });
  }
}