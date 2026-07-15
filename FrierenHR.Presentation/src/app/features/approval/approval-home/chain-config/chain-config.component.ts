import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { DropdownModule } from 'primeng/dropdown';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { ApprovalService } from '../../../../core/services/approval.service';
import { EMPLOYEE_ROLES } from '../../../../core/models/enums.model';

@Component({
  selector: 'app-chain-config',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputTextModule, InputNumberModule, DropdownModule, ButtonModule, MessageModule],
  templateUrl: './chain-config.component.html',
  styleUrl: './chain-config.component.scss',
})
export class ChainConfigComponent {
  private readonly fb = inject(FormBuilder);
  private readonly approvalService = inject(ApprovalService);

  readonly roles = EMPLOYEE_ROLES;
  readonly saving = signal(false);
  readonly saveError = signal<string | null>(null);
  readonly saveSuccess = signal(false);

  form = this.fb.group({
    companyId: ['', Validators.required],
    name: ['Standard Leave Approval', Validators.required],
    steps: this.fb.array([this.newStep(1, 'Manager', 3)]),
  });

  get steps(): FormArray { return this.form.get('steps') as FormArray; }

  newStep(stepOrder: number, approverRole: string, escalateAfterDays: number) {
    return this.fb.group({
      stepOrder: [stepOrder, Validators.required],
      approverRole: [approverRole, Validators.required],
      escalateAfterDays: [escalateAfterDays, Validators.required],
    });
  }

  addStep(): void { this.steps.push(this.newStep(this.steps.length + 1, 'HRAdmin', 3)); }
  removeStep(index: number): void { this.steps.removeAt(index); }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.saveError.set(null);
    this.saveSuccess.set(false);
    this.approvalService.create(this.form.getRawValue() as any).subscribe({
      next: () => { this.saving.set(false); this.saveSuccess.set(true); },
      error: (err) => { this.saving.set(false); this.saveError.set(err?.error?.message ?? 'Save failed.'); },
    });
  }
}