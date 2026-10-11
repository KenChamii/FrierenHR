import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { ReportingService } from '../../../core/services/reporting.service';
import { PayrollPeriodDto } from '../../../core/models/reporting.model';
import { saveBlob } from '../../../core/utils/download';
import { toIsoDate } from '../../../core/utils/dates';

@Component({
  selector: 'app-payroll-period',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, MessageModule, TableModule],
  templateUrl: './payroll-period.component.html',
  styleUrl: './payroll-period.component.scss',
})
export class PayrollPeriodComponent implements OnInit {
  private readonly reports = inject(ReportingService);

  readonly data = signal<PayrollPeriodDto | null>(null);
  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly error = signal<string | null>(null);

  from = '';
  to = '';

  readonly totals = computed(() => {
    const rows = this.data()?.rows ?? [];
    return {
      people: rows.length,
      regular: this.sum(rows.map(r => r.regularHours)),
      overtime: this.sum(rows.map(r => r.overtimeHours)),
      payable: this.sum(rows.map(r => r.payableOvertimeHours)),
      late: this.sum(rows.map(r => r.lateMinutes)),
      leave: this.sum(rows.map(r => r.leaveDaysTaken)),
    };
  });

  ngOnInit(): void {
    // Defaults to the current month, the period most often asked for.
    const today = new Date();
    this.from = toIsoDate(new Date(today.getFullYear(), today.getMonth(), 1));
    this.to = toIsoDate(new Date(today.getFullYear(), today.getMonth() + 1, 0));
    this.load();
  }

  load(): void {
    if (!this.isRangeValid()) return;

    this.loading.set(true);
    this.error.set(null);
    this.reports.payrollPeriod(this.from, this.to).subscribe({
      next: data => { this.data.set(data); this.loading.set(false); },
      error: err => {
        this.loading.set(false);
        this.error.set(err?.error ?? 'Could not build the period.');
      },
    });
  }

  download(): void {
    if (!this.isRangeValid()) return;

    this.exporting.set(true);
    this.error.set(null);
    this.reports.payrollPeriodCsv(this.from, this.to).subscribe({
      next: blob => {
        this.exporting.set(false);
        saveBlob(blob, `payroll-${this.from}-to-${this.to}.csv`);
      },
      error: () => { this.exporting.set(false); this.error.set('Could not export the file.'); },
    });
  }

  private isRangeValid(): boolean {
    if (!this.from || !this.to) { this.error.set('Pick both dates.'); return false; }
    if (this.to < this.from) { this.error.set('The end date cannot be before the start date.'); return false; }
    return true;
  }

  private sum(values: number[]): number {
    return Math.round(values.reduce((total, value) => total + value, 0) * 100) / 100;
  }
}
