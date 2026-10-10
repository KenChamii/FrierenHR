import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ShiftService } from '../../../core/services/scheduling.service';
import { AuthService } from '../../../core/services/auth.service';
import { SaveShiftDto, ShiftDto, WEEK_DAYS, toggleWeekDay, weekDaysToList } from '../../../core/models/scheduling.model';

@Component({
  selector: 'app-shifts',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule, ButtonModule, InputNumberModule,
    InputTextModule, MessageModule, TableModule, TagModule, ToggleSwitchModule,
  ],
  templateUrl: './shifts.component.html',
  styleUrl: './shifts.component.scss',
})
export class ShiftsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly shifts = inject(ShiftService);
  private readonly auth = inject(AuthService);

  readonly weekDays = WEEK_DAYS;

  readonly list = signal<ShiftDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);
  readonly editingId = signal<string | null>(null);

  /** Rest days are a bit field, edited as toggles rather than a multi-select. */
  readonly restDays = signal<number>(WEEK_DAYS[5].value | WEEK_DAYS[6].value);

  form = this.fb.group({
    name: this.fb.nonNullable.control('', Validators.required),
    startTime: this.fb.nonNullable.control('08:00', Validators.required),
    endTime: this.fb.nonNullable.control('17:00', Validators.required),
    breakMinutes: this.fb.nonNullable.control(60),
    isDefault: this.fb.nonNullable.control(false),
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.shifts.getMine().subscribe({
      next: list => { this.list.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); this.error.set('Could not load shifts.'); },
    });
  }

  toggleDay(day: number): void {
    this.restDays.update(current => toggleWeekDay(current, day));
  }

  isRestDay(day: number): boolean {
    return (this.restDays() & day) !== 0;
  }

  restDayLabels(flags: number): string {
    const days = weekDaysToList(flags);
    return days.length ? days.join(', ') : 'None';
  }

  edit(shift: ShiftDto): void {
    this.editingId.set(shift.id);
    this.error.set(null);
    this.notice.set(null);
    this.restDays.set(shift.restDays);
    this.form.setValue({
      name: shift.name,
      // The API sends "08:00:00"; the time input wants "08:00".
      startTime: shift.startTime.slice(0, 5),
      endTime: shift.endTime.slice(0, 5),
      breakMinutes: shift.breakMinutes,
      isDefault: shift.isDefault,
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.restDays.set(WEEK_DAYS[5].value | WEEK_DAYS[6].value);
    this.form.reset({ name: '', startTime: '08:00', endTime: '17:00', breakMinutes: 60, isDefault: false });
  }

  save(): void {
    const companyId = this.auth.currentCompanyId();
    if (!companyId || this.form.invalid) { this.form.markAllAsTouched(); return; }

    const raw = this.form.getRawValue();
    const dto: SaveShiftDto = {
      id: this.editingId() ?? undefined,
      companyId,
      name: raw.name,
      startTime: `${raw.startTime}:00`,
      endTime: `${raw.endTime}:00`,
      breakMinutes: raw.breakMinutes,
      restDays: this.restDays(),
      isDefault: raw.isDefault,
    };

    this.saving.set(true);
    this.error.set(null);
    this.shifts.save(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.notice.set(this.editingId() ? 'Shift updated.' : 'Shift added.');
        this.cancelEdit();
        this.load();
      },
      error: err => { this.saving.set(false); this.error.set(err?.error?.message ?? 'Could not save.'); },
    });
  }

  remove(shift: ShiftDto): void {
    this.error.set(null);
    this.shifts.delete(shift.id).subscribe({
      next: () => { this.notice.set(`Removed ${shift.name}.`); this.load(); },
      // The API refuses while people are assigned and says how many, so show that verbatim.
      error: err => this.error.set(err?.error?.message ?? 'Could not delete.'),
    });
  }

  /** Hours actually worked, so someone can sanity-check the shift they just defined. */
  workedHours(shift: ShiftDto): string {
    const start = this.minutesOf(shift.startTime);
    const end = this.minutesOf(shift.endTime);
    const span = end > start ? end - start : 24 * 60 - (start - end);
    return ((span - shift.breakMinutes) / 60).toFixed(2);
  }

  private minutesOf(time: string): number {
    const [h, m] = time.split(':').map(Number);
    return h * 60 + m;
  }
}
