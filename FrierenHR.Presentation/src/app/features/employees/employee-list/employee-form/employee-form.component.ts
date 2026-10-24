import { Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { DropdownModule } from 'primeng/dropdown';
import { InputNumberModule } from 'primeng/inputnumber';
import { DatePickerModule } from 'primeng/datepicker';
import { ButtonModule } from 'primeng/button';
import { EmployeeService } from '../../../../core/services/employee.service';
import { AuthService } from '../../../../core/services/auth.service';
import { CompanyService } from '../../../../core/services/company.service';
import { EmployeeDto } from '../../../../core/models/employee.model';
import { DepartmentDto } from '../../../../core/models/company.model';
import { EMPLOYEE_ROLES } from '../../../../core/models/enums.model';
import { toLocalDateTime } from '../../../../core/utils/dates';

@Component({
  selector: 'app-employee-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputTextModule, PasswordModule, DropdownModule, InputNumberModule, DatePickerModule, ButtonModule],
  templateUrl: './employee-form.component.html',
  styleUrl: './employee-form.component.scss',
})
export class EmployeeFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly employeeService = inject(EmployeeService);
  private readonly authService = inject(AuthService);
  private readonly companyService = inject(CompanyService);

  @Input() employee: EmployeeDto | null = null;
  @Output() saved = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  readonly roles = EMPLOYEE_ROLES;
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly departments = signal<DepartmentDto[]>([]);
  readonly managers = signal<EmployeeDto[]>([]);

  form = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
    hireDate: [new Date(), Validators.required],
    role: ['Employee', Validators.required],
    departmentId: [null as string | null],
    managerId: [null as string | null],
    // Optional.
    monthlyRate: [null as number | null, Validators.min(0)],
    standardMonthlyHours: [174, [Validators.required, Validators.min(1)]],
  });

  // Mirrors PasswordPolicy on the backend (min 8 chars, at least one letter and one digit).
  private readonly passwordValidators = [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d).+$/)];

  ngOnInit(): void {
    if (this.employee) {
      this.form.patchValue({
        firstName: this.employee.firstName, lastName: this.employee.lastName,
        hireDate: new Date(this.employee.hireDate), role: this.employee.role,
        departmentId: this.employee.departmentId ?? null, managerId: this.employee.managerId ?? null,
        monthlyRate: this.employee.monthlyRate ?? null,
        standardMonthlyHours: this.employee.standardMonthlyHours ?? 174,
      });
      this.form.get('email')?.disable();
      this.form.get('password')?.clearValidators();
    } else {
      this.form.get('password')?.setValidators(this.passwordValidators);
    }

    const companyId = this.authService.currentCompanyId();
    if (companyId) {
      // Both lookups report their own failure.
      this.companyService.getDepartments(companyId).subscribe({
        next: (list) => this.departments.set(list),
        error: () => this.errorMessage.set('Could not load departments.'),
      });
      this.employeeService.getByCompany(companyId).subscribe({
        // Can't be your own manager when editing — otherwise a manager loop would be trivial to create.
        next: (list) => this.managers.set(this.employee ? list.filter((e) => e.id !== this.employee!.id) : list),
        error: () => this.errorMessage.set('Could not load the manager list.'),
      });
    }
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.errorMessage.set(null);
    const raw = this.form.getRawValue();

    const request$ = this.employee
      ? this.employeeService.update(this.employee.id, {
          departmentId: raw.departmentId ?? undefined, managerId: raw.managerId ?? undefined,
          firstName: raw.firstName!, lastName: raw.lastName!, role: raw.role as any, isActive: true,
          monthlyRate: raw.monthlyRate ?? null, standardMonthlyHours: raw.standardMonthlyHours ?? 174,
        })
      : this.employeeService.create({
          companyId: this.authService.currentCompanyId() ?? '',
          departmentId: raw.departmentId ?? undefined, managerId: raw.managerId ?? undefined,
          firstName: raw.firstName!, lastName: raw.lastName!, email: raw.email!,
          password: raw.password!, hireDate: toLocalDateTime(raw.hireDate as Date), role: raw.role as any,
          monthlyRate: raw.monthlyRate ?? null, standardMonthlyHours: raw.standardMonthlyHours ?? 174,
        });

    request$.subscribe({
      next: () => { this.saving.set(false); this.saved.emit(); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err?.error?.message ?? 'Save failed.'); },
    });
  }
}
