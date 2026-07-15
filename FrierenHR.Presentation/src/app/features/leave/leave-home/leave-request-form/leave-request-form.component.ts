import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePickerModule } from 'primeng/datepicker';
import { DropdownModule } from 'primeng/dropdown';
import { TextareaModule } from 'primeng/textarea';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { LeaveService } from '../../../../core/services/leave.service';
import { AuthService } from '../../../../core/services/auth.service';
import { LEAVE_TYPES } from '../../../../core/models/enums.model';
import { LeaveRequestDto } from '../../../../core/models/leave.model';

@Component({
  selector: 'app-leave-request-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePickerModule, DropdownModule, TextareaModule, ButtonModule, MessageModule],
  templateUrl: './leave-request-form.component.html',
  styleUrl: './leave-request-form.component.scss',
})
export class LeaveRequestFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly leaveService = inject(LeaveService);
  private readonly authService = inject(AuthService);

  readonly leaveTypes = LEAVE_TYPES;
  readonly submitting = signal(false);
  readonly result = signal<LeaveRequestDto | null>(null);
  readonly errorMessage = signal<string | null>(null);

  form = this.fb.group({
    leaveType: ['Vacation', Validators.required],
    startDate: [new Date(), Validators.required],
    endDate: [new Date(), Validators.required],
    reason: [''],
  });

  submit(): void {
    if (this.form.invalid) return;
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) return;

    this.submitting.set(true);
    this.result.set(null);
    this.errorMessage.set(null);

    const raw = this.form.getRawValue();
    this.leaveService.requestLeave({
      employeeId, leaveType: raw.leaveType as any,
      startDate: (raw.startDate as Date).toISOString(), endDate: (raw.endDate as Date).toISOString(),
      reason: raw.reason || undefined,
    }).subscribe({
      next: (created) => { this.submitting.set(false); this.result.set(created); this.form.reset({ leaveType: 'Vacation', startDate: new Date(), endDate: new Date() }); },
      error: (err) => { this.submitting.set(false); this.errorMessage.set(err?.error?.message ?? 'Request failed.'); },
    });
  }
}