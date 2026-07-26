import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { EmployeeService } from '../../core/services/employee.service';
import { AuthService } from '../../core/services/auth.service';
import { EmployeeDto } from '../../core/models/employee.model';

@Component({
  selector: 'app-my-team',
  standalone: true,
  imports: [CommonModule, TableModule, TagModule],
  templateUrl: './my-team.component.html',
  styleUrl: './my-team.component.scss',
})
export class MyTeamComponent implements OnInit {
  readonly reports = signal<EmployeeDto[]>([]);
  readonly loading = signal(true);

  constructor(private employeeService: EmployeeService, private authService: AuthService) {}

  ngOnInit(): void {
    const managerId = this.authService.currentEmployeeId();
    if (!managerId) { this.loading.set(false); return; }
    this.employeeService.getDirectReports(managerId).subscribe({
      next: (list) => { this.reports.set(list); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
