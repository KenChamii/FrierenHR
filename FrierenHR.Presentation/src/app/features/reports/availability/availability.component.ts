import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { MessageModule } from 'primeng/message';
import { ReportingService } from '../../../core/services/reporting.service';
import { CompanyService } from '../../../core/services/company.service';
import { AuthService } from '../../../core/services/auth.service';
import { DepartmentDto } from '../../../core/models/company.model';
import {
  AVAILABILITY_LEGEND, AvailabilityDayDto, AvailabilityDto, DayAvailability,
} from '../../../core/models/reporting.model';
import { toIsoDate } from '../../../core/utils/download';

@Component({
  selector: 'app-availability',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, DropdownModule, MessageModule],
  templateUrl: './availability.component.html',
  styleUrl: './availability.component.scss',
})
export class AvailabilityComponent implements OnInit {
  private readonly reports = inject(ReportingService);
  private readonly companies = inject(CompanyService);
  private readonly auth = inject(AuthService);

  readonly legend = AVAILABILITY_LEGEND;

  readonly data = signal<AvailabilityDto | null>(null);
  readonly departments = signal<DepartmentDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  /** Two weeks reads as a planning window; a month of columns does not fit a screen. */
  readonly spanDays = signal(14);
  readonly departmentId = signal<string | null>(null);
  readonly from = signal(toIsoDate(new Date()));

  readonly spanOptions = [
    { label: 'Next 7 days', value: 7 },
    { label: 'Next 14 days', value: 14 },
    { label: 'Next 30 days', value: 30 },
  ];

  readonly departmentOptions = computed(() => [
    { label: 'Whole company', value: null as string | null },
    ...this.departments().map(d => ({ label: d.name, value: d.id as string | null })),
  ]);

  /** Column headers come from the first row, so every row stays aligned to one date list. */
  readonly dates = computed(() => this.data()?.rows[0]?.days.map(d => d.date) ?? []);

  readonly outToday = computed(() => {
    const today = toIsoDate(new Date());
    return (this.data()?.rows ?? [])
      .filter(row => row.days.some(d => d.date === today && d.status === 'OnLeave'))
      .map(row => row.employeeName);
  });

  ngOnInit(): void {
    const companyId = this.auth.currentCompanyId();
    if (companyId) {
      this.companies.getDepartments(companyId).subscribe({
        next: list => this.departments.set(list),
        error: () => this.departments.set([]),
      });
    }
    this.load();
  }

  load(): void {
    const start = new Date(this.from());
    const end = new Date(start);
    end.setDate(end.getDate() + this.spanDays() - 1);

    this.loading.set(true);
    this.error.set(null);
    this.reports.availability(toIsoDate(start), toIsoDate(end), this.departmentId()).subscribe({
      next: data => { this.data.set(data); this.loading.set(false); },
      error: err => {
        this.loading.set(false);
        this.error.set(err?.error ?? 'Could not load availability.');
      },
    });
  }

  onSpanChange(days: number): void {
    this.spanDays.set(days);
    this.load();
  }

  onDepartmentChange(departmentId: string | null): void {
    this.departmentId.set(departmentId);
    this.load();
  }

  shift(weeks: number): void {
    const start = new Date(this.from());
    start.setDate(start.getDate() + weeks * 7);
    this.from.set(toIsoDate(start));
    this.load();
  }

  today(): void {
    this.from.set(toIsoDate(new Date()));
    this.load();
  }

  /** Short day and date, so a fortnight of columns stays readable. */
  headerFor(date: string): { weekday: string; day: string } {
    const [y, m, d] = date.split('-').map(Number);
    const parsed = new Date(y, m - 1, d);
    return {
      weekday: parsed.toLocaleDateString(undefined, { weekday: 'narrow' }),
      day: `${d}`.padStart(2, '0'),
    };
  }

  classFor(day: AvailabilityDayDto): string {
    return `cell ${day.status.toLowerCase()}`;
  }

  titleFor(day: AvailabilityDayDto): string {
    const label = this.legend.find(l => l.value === day.status)?.label ?? day.status;
    return day.detail ? `${label}: ${day.detail}` : label;
  }

  legendClass(status: DayAvailability): string {
    return `swatch ${status.toLowerCase()}`;
  }
}
