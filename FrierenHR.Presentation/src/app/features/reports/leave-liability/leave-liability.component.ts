import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { ReportingService } from '../../../core/services/reporting.service';
import { LiabilityDto } from '../../../core/models/reporting.model';
import { saveBlob } from '../../../core/utils/download';
import { toIsoDate } from '../../../core/utils/dates';

@Component({
  selector: 'app-leave-liability',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputNumberModule, MessageModule, TableModule],
  templateUrl: './leave-liability.component.html',
  styleUrl: './leave-liability.component.scss',
})
export class LeaveLiabilityComponent {
  private readonly reports = inject(ReportingService);

  readonly data = signal<LiabilityDto | null>(null);
  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly error = signal<string | null>(null);

  /**
   * Entered rather than read from configuration: no salary is held against an employee, so a
   * single rate the user supplies is honest about what the figure is based on.
   */
  dailyRate: number | null = null;

  run(): void {
    if (!this.isRateValid()) return;

    this.loading.set(true);
    this.error.set(null);
    this.reports.leaveLiability(this.dailyRate!).subscribe({
      next: data => { this.data.set(data); this.loading.set(false); },
      error: err => {
        this.loading.set(false);
        this.error.set(err?.error ?? 'Could not build the report.');
      },
    });
  }

  download(): void {
    if (!this.isRateValid()) return;

    this.exporting.set(true);
    this.error.set(null);
    this.reports.leaveLiabilityCsv(this.dailyRate!).subscribe({
      next: blob => {
        this.exporting.set(false);
        saveBlob(blob, `leave-liability-${toIsoDate(new Date())}.csv`);
      },
      error: () => { this.exporting.set(false); this.error.set('Could not export the file.'); },
    });
  }

  private isRateValid(): boolean {
    if (this.dailyRate === null || this.dailyRate <= 0) {
      this.error.set('Enter a daily rate above zero.');
      return false;
    }
    return true;
  }
}
