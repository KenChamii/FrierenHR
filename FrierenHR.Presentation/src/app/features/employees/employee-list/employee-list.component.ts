import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { EmployeeService } from '../../../core/services/employee.service';
import { AuthService } from '../../../core/services/auth.service';
import { EmployeeDto } from '../../../core/models/employee.model';
import { EmployeeFormComponent } from './employee-form/employee-form.component';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, TagModule, DialogModule, EmployeeFormComponent],
  templateUrl: './employee-list.component.html',
  styleUrl: './employee-list.component.scss',
})
export class EmployeeListComponent implements OnInit {
  readonly employees = signal<EmployeeDto[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly editingEmployee = signal<EmployeeDto | null>(null);

  constructor(private employeeService: EmployeeService, private authService: AuthService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    // NOTE: companyId should come from the logged-in employee's own record (EmployeeDto.companyId),
    // fetched once at login and cached — placeholder empty string here, wire it up once auth returns companyId.
    const companyId = '';
    this.loading.set(true);
    this.employeeService.getByCompany(companyId).subscribe({
      next: (list) => { this.employees.set(list); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  openCreate(): void { this.editingEmployee.set(null); this.dialogVisible.set(true); }
  openEdit(employee: EmployeeDto): void { this.editingEmployee.set(employee); this.dialogVisible.set(true); }

  onSaved(): void { this.dialogVisible.set(false); this.load(); }

  severityFor(role: string): 'success' | 'info' | 'warn' {
    return role === 'HRAdmin' ? 'warn' : role === 'Manager' ? 'info' : 'success';
  }
}