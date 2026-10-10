import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DropdownModule } from 'primeng/dropdown';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { ReportingService } from '../../../core/services/reporting.service';
import { LeaveForecastDto, LeaveProjectionDto } from '../../../core/models/reporting.model';
import { toIsoDate } from '../../../core/utils/download';

@Component({
  selector: 'app-leave-forecast',
  standalone: true,
  imports: [CommonModule, FormsModule, DropdownModule, MessageModule, TagModule],
  templateUrl: './leave-forecast.component.html',
  styleUrl: './leave-forecast.component.scss',
})
export class LeaveForecastComponent implements OnInit {
  private readonly reports = inject(ReportingService);

  readonly data = signal<LeaveForecastDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly months = signal(12);

  readonly horizonOptions = [
    { label: 'Next 6 months', value: 6 },
    { label: 'Next 12 months', value: 12 },
    { label: 'Next 24 months', value: 24 },
  ];

  /** The point of the screen: leave that will be lost unless it is booked. */
  readonly atRisk = computed(() =>
    (this.data()?.projections ?? []).filter(p => p.forfeited > 0));

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const horizon = new Date();
    horizon.setMonth(horizon.getMonth() + this.months());

    this.loading.set(true);
    this.error.set(null);
    this.reports.leaveForecast(undefined, toIsoDate(horizon)).subscribe({
      next: data => { this.data.set(data); this.loading.set(false); },
      error: err => {
        this.loading.set(false);
        this.error.set(err?.error ?? 'Could not load the forecast.');
      },
    });
  }

  onHorizonChange(months: number): void {
    this.months.set(months);
    this.load();
  }

  /** How full the balance is against its cap, for the bar. */
  fillPercent(projection: LeaveProjectionDto): number {
    if (!projection.cap || projection.cap <= 0) return 0;
    return Math.min(100, Math.round((projection.projectedBalance / projection.cap) * 100));
  }

  gained(projection: LeaveProjectionDto): number {
    return Math.max(0, projection.projectedBalance - projection.currentBalance);
  }
}
