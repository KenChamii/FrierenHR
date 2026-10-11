import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { PayrollDeductionService } from '../../../core/services/payroll.service';
import { AuthService } from '../../../core/services/auth.service';
import {
  DEDUCTION_KINDS, DeductionKind, PayrollDeductionDto, SavePayrollDeductionDto,
} from '../../../core/models/payroll.model';

@Component({
  selector: 'app-payroll-deductions',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule, ButtonModule, DropdownModule,
    InputNumberModule, InputTextModule, MessageModule, TableModule, TagModule,
  ],
  templateUrl: './deductions.component.html',
  styleUrl: './deductions.component.scss',
})
export class PayrollDeductionsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly deductions = inject(PayrollDeductionService);
  private readonly auth = inject(AuthService);

  readonly kinds = DEDUCTION_KINDS;

  readonly list = signal<PayrollDeductionDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);
  readonly editingId = signal<string | null>(null);

  form = this.fb.group({
    name: this.fb.nonNullable.control('', Validators.required),
    kind: this.fb.nonNullable.control<DeductionKind>('FixedAmount'),
    value: this.fb.nonNullable.control(0, [Validators.required, Validators.min(0)]),
    sortOrder: this.fb.nonNullable.control(0),
    isActive: this.fb.nonNullable.control(true),
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.deductions.getAll().subscribe({
      next: list => { this.list.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); this.error.set('Could not load deductions.'); },
    });
  }

  edit(deduction: PayrollDeductionDto): void {
    this.editingId.set(deduction.id);
    this.error.set(null);
    this.notice.set(null);
    this.form.setValue({
      name: deduction.name,
      kind: deduction.kind,
      value: deduction.value,
      sortOrder: deduction.sortOrder,
      isActive: deduction.isActive,
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.form.reset({ name: '', kind: 'FixedAmount', value: 0, sortOrder: 0, isActive: true });
  }

  save(): void {
    const companyId = this.auth.currentCompanyId();
    if (!companyId || this.form.invalid) { this.form.markAllAsTouched(); return; }

    const raw = this.form.getRawValue();
    const dto: SavePayrollDeductionDto = {
      id: this.editingId() ?? undefined,
      companyId,
      name: raw.name,
      kind: raw.kind,
      value: raw.value,
      sortOrder: raw.sortOrder,
      isActive: raw.isActive,
    };

    this.saving.set(true);
    this.error.set(null);
    this.deductions.save(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.notice.set(this.editingId() ? 'Deduction updated.' : 'Deduction added.');
        this.cancelEdit();
        this.load();
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.message ?? 'Could not save.');
      },
    });
  }

  retire(deduction: PayrollDeductionDto): void {
    this.error.set(null);
    this.deductions.delete(deduction.id).subscribe({
      next: () => {
        this.notice.set(`${deduction.name} will not appear on future payslips.`);
        this.load();
      },
      error: err => this.error.set(err?.error?.message ?? 'Could not retire that line.'),
    });
  }

  labelFor(kind: DeductionKind): string {
    return this.kinds.find(k => k.value === kind)?.label ?? kind;
  }

  valueLabel(deduction: PayrollDeductionDto): string {
    return deduction.kind === 'PercentOfGross'
      ? `${deduction.value}% of gross`
      : deduction.value.toFixed(2);
  }
}
