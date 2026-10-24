import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { PayslipService } from '../../../core/services/payroll.service';
import { AuthService } from '../../../core/services/auth.service';
import { PayslipDto, PayslipSummaryDto } from '../../../core/models/payroll.model';
import { toIsoDate } from '../../../core/utils/dates';

@Component({
  selector: 'app-payslips',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, MessageModule, TableModule],
  templateUrl: './payslips.component.html',
  styleUrl: './payslips.component.scss',
})
export class PayslipsComponent implements OnInit {
  private readonly payslips = inject(PayslipService);
  private readonly auth = inject(AuthService);

  readonly rows = signal<PayslipSummaryDto[]>([]);
  readonly selected = signal<PayslipDto | null>(null);
  readonly loading = signal(true);
  readonly generating = signal(false);
  readonly error = signal<string | null>(null);
  readonly skipped = signal<string[]>([]);

  readonly isHr = computed(() => this.auth.currentRole() === 'HRAdmin');

  /** HR looks at a period across everyone; an employee looks at their own history. */
  readonly scope = signal<'mine' | 'period'>('mine');

  from = '';
  to = '';

  readonly earnings = computed(() =>
    (this.selected()?.lines ?? []).filter(l => l.kind === 'Earning'));

  readonly deductions = computed(() =>
    (this.selected()?.lines ?? []).filter(l => l.kind === 'Deduction'));

  ngOnInit(): void {
    // Defaults to the second half of the current month, the commoner of the two PH cut-offs.
    const today = new Date();
    this.from = toIsoDate(new Date(today.getFullYear(), today.getMonth(), 16));
    this.to = toIsoDate(new Date(today.getFullYear(), today.getMonth() + 1, 0));
    this.loadMine();
  }

  loadMine(): void {
    this.scope.set('mine');
    this.loading.set(true);
    this.error.set(null);
    this.payslips.mine().subscribe({
      next: list => { this.rows.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); this.error.set('Could not load your payslips.'); },
    });
  }

  loadPeriod(): void {
    if (!this.isRangeValid()) return;

    this.scope.set('period');
    this.loading.set(true);
    this.error.set(null);
    this.payslips.forPeriod(this.from, this.to).subscribe({
      next: list => { this.rows.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); this.error.set('Could not load that period.'); },
    });
  }

  generate(): void {
    if (!this.isRangeValid()) return;

    this.generating.set(true);
    this.error.set(null);
    this.skipped.set([]);

    this.payslips.generate(this.from, this.to).subscribe({
      next: run => {
        this.generating.set(false);
        this.scope.set('period');
        this.rows.set(run.payslips);
        // Surfaced, not swallowed.
        this.skipped.set(run.skipped);
      },
      error: err => {
        this.generating.set(false);
        this.error.set(err?.error?.message ?? 'Could not generate payslips.');
      },
    });
  }

  open(row: PayslipSummaryDto): void {
    this.error.set(null);
    this.payslips.getById(row.id).subscribe({
      next: payslip => this.selected.set(payslip),
      error: () => this.error.set('Could not open that payslip.'),
    });
  }

  close(): void { this.selected.set(null); }

  private isRangeValid(): boolean {
    if (!this.from || !this.to) { this.error.set('Pick both dates.'); return false; }
    if (this.to < this.from) { this.error.set('The end date cannot be before the start date.'); return false; }
    return true;
  }
}
