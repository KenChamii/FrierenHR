import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { CompanyService } from '../../../core/services/company.service';
import { AuthService } from '../../../core/services/auth.service';
import { DepartmentDto } from '../../../core/models/company.model';

@Component({
  selector: 'app-departments',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TableModule, ButtonModule, InputTextModule, MessageModule],
  templateUrl: './departments.component.html',
  styleUrl: './departments.component.scss',
})
export class DepartmentsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly companyService = inject(CompanyService);
  private readonly authService = inject(AuthService);

  readonly departments = signal<DepartmentDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  // inject() resolves immediately (no constructor-ordering issue), so this field
  // initializer can safely use `this.fb` right away — unlike constructor-parameter
  // injection, where field initializers run before the parameter is assigned.
  form = this.fb.group({ name: ['', Validators.required] });

  ngOnInit(): void { this.load(); }

  load(): void {
    const companyId = this.authService.currentCompanyId();
    if (!companyId) { this.loading.set(false); return; }
    this.loading.set(true);
    this.companyService.getDepartments(companyId).subscribe({
      next: (list) => { this.departments.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); this.errorMessage.set('Could not load departments.'); },
    });
  }

  add(): void {
    if (this.form.invalid) return;
    const companyId = this.authService.currentCompanyId();
    if (!companyId) return;
    this.saving.set(true);
    this.errorMessage.set(null);
    this.companyService.createDepartment(companyId, { name: this.form.value.name! }).subscribe({
      next: () => { this.saving.set(false); this.form.reset(); this.load(); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err?.error?.message ?? 'Could not add department.'); },
    });
  }
}
