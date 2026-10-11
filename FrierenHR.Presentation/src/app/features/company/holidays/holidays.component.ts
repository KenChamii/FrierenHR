import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { HolidayService } from '../../../core/services/scheduling.service';
import { CompanyService } from '../../../core/services/company.service';
import { AuthService } from '../../../core/services/auth.service';
import { DepartmentDto } from '../../../core/models/company.model';
import {
  HOLIDAY_TYPES, HolidayDto, HolidaySuggestionDto, HolidayType, SaveHolidayDto,
} from '../../../core/models/scheduling.model';
import { CheckboxModule } from 'primeng/checkbox';

/** A suggested holiday with the choices an administrator can change before importing. */
interface SuggestionRow extends HolidaySuggestionDto {
  include: boolean;
  type: HolidayType;
}

@Component({
  selector: 'app-holidays',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule, ButtonModule, DropdownModule,
    InputTextModule, MessageModule, TableModule, TagModule, TextareaModule, CheckboxModule,
  ],
  templateUrl: './holidays.component.html',
  styleUrl: './holidays.component.scss',
})
export class HolidaysComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly holidays = inject(HolidayService);
  private readonly companies = inject(CompanyService);
  private readonly auth = inject(AuthService);

  readonly holidayTypes = HOLIDAY_TYPES;

  readonly list = signal<HolidayDto[]>([]);
  readonly departments = signal<DepartmentDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly importing = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);
  readonly editingId = signal<string | null>(null);
  readonly showImport = signal(false);

  // --- Fetching a year from the holiday service ---

  readonly showFetch = signal(false);
  readonly fetching = signal(false);
  readonly suggestions = signal<SuggestionRow[]>([]);
  readonly suggestionSource = signal<string | null>(null);
  readonly suggestionCaveat = signal<string | null>(null);

  readonly selectedSuggestions = computed(() => this.suggestions().filter(s => s.include));

  readonly year = signal(new Date().getFullYear());
  readonly years = computed(() => {
    const current = new Date().getFullYear();
    return [current - 1, current, current + 1, current + 2].map(y => ({ label: String(y), value: y }));
  });

  /** Company-wide first, then department-specific, which is how people read the calendar. */
  readonly departmentOptions = computed(() => [
    { label: 'Whole company', value: null as string | null },
    ...this.departments().map(d => ({ label: d.name, value: d.id as string | null })),
  ]);

  form = this.fb.group({
    date: this.fb.nonNullable.control('', Validators.required),
    name: this.fb.nonNullable.control('', Validators.required),
    type: this.fb.nonNullable.control<HolidayType>('Regular'),
    departmentId: this.fb.control<string | null>(null),
  });

  /** One "YYYY-MM-DD, Name, Type" per line. Type is optional and defaults to Regular. */
  importText = '';

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
    this.loading.set(true);
    this.holidays.getForYear(this.year()).subscribe({
      next: list => { this.list.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); this.error.set('Could not load the calendar.'); },
    });
  }

  onYearChange(year: number): void {
    this.year.set(year);
    this.load();
  }

  edit(holiday: HolidayDto): void {
    this.editingId.set(holiday.id);
    this.error.set(null);
    this.notice.set(null);
    this.form.setValue({
      date: holiday.date,
      name: holiday.name,
      type: holiday.type,
      departmentId: holiday.departmentId ?? null,
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.form.reset({ date: '', name: '', type: 'Regular', departmentId: null });
  }

  save(): void {
    const companyId = this.auth.currentCompanyId();
    if (!companyId || this.form.invalid) { this.form.markAllAsTouched(); return; }

    const raw = this.form.getRawValue();
    const dto: SaveHolidayDto = {
      id: this.editingId() ?? undefined,
      companyId,
      date: raw.date,
      name: raw.name,
      type: raw.type,
      departmentId: raw.departmentId,
    };

    this.saving.set(true);
    this.error.set(null);
    this.holidays.save(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.notice.set(this.editingId() ? 'Holiday updated.' : 'Holiday added.');
        this.cancelEdit();
        this.load();
      },
      error: err => { this.saving.set(false); this.error.set(err?.error?.message ?? 'Could not save.'); },
    });
  }

  remove(holiday: HolidayDto): void {
    this.error.set(null);
    this.holidays.delete(holiday.id).subscribe({
      next: () => { this.notice.set(`Removed ${holiday.name}.`); this.load(); },
      error: err => this.error.set(err?.error?.message ?? 'Could not delete.'),
    });
  }

  /**
   * Pasting a year's calendar is how this actually gets filled in, so the import takes the
   * shape people already have it in rather than requiring 15 separate forms.
   */
  runImport(): void {
    const companyId = this.auth.currentCompanyId();
    if (!companyId) return;

    const rows = this.parseImport(companyId);
    if (!rows.length) { this.error.set('Nothing to import. One holiday per line: 2026-01-01, New Year, Regular'); return; }

    this.importing.set(true);
    this.error.set(null);
    this.holidays.import(rows).subscribe({
      next: result => {
        this.importing.set(false);
        this.notice.set(`Added ${result.added}, skipped ${result.skipped}.`);
        this.importText = '';
        this.showImport.set(false);
        this.load();
      },
      error: err => { this.importing.set(false); this.error.set(err?.error?.message ?? 'Import failed.'); },
    });
  }

  private parseImport(companyId: string): SaveHolidayDto[] {
    return this.importText
      .split('\n')
      .map(line => line.trim())
      .filter(line => line.length > 0)
      .map(line => {
        const [date, name, type] = line.split(',').map(part => part?.trim());
        return { companyId, date, name: name ?? '', type: this.asType(type) };
      })
      .filter(row => /^\d{4}-\d{2}-\d{2}$/.test(row.date ?? '') && row.name.length > 0);
  }

  private asType(value: string | undefined): HolidayType {
    const match = HOLIDAY_TYPES.find(t => t.value.toLowerCase() === value?.toLowerCase());
    return match?.value ?? 'Regular';
  }

  labelFor(type: HolidayType): string {
    return HOLIDAY_TYPES.find(t => t.value === type)?.label ?? type;
  }

  /**
   * Fetches a year for review. Nothing is written here: the list is a starting point, not
   * an authority, and the types in particular need checking before they affect pay.
   */
  fetchSuggestions(): void {
    this.fetching.set(true);
    this.error.set(null);
    this.notice.set(null);

    this.holidays.suggestions(this.year()).subscribe({
      next: result => {
        this.fetching.set(false);
        this.suggestionSource.set(result.source);
        this.suggestionCaveat.set(result.caveat);
        this.suggestions.set(result.holidays.map(h => ({
          ...h,
          // A date already on the calendar is shown but not ticked, so importing cannot
          // quietly add a second entry for it.
          include: !h.alreadyRecorded,
          type: h.suggestedType,
        })));
        this.showFetch.set(true);
      },
      error: err => {
        this.fetching.set(false);
        this.error.set(err?.error?.message ?? 'Could not fetch that year.');
      },
    });
  }

  toggleSuggestion(row: SuggestionRow): void {
    this.updateSuggestion(row, { include: !row.include });
  }

  setSuggestionType(row: SuggestionRow, type: HolidayType): void {
    this.updateSuggestion(row, { type });
  }

  private updateSuggestion(row: SuggestionRow, changes: Partial<SuggestionRow>): void {
    this.suggestions.update(rows =>
      rows.map(r => (r.date === row.date ? { ...r, ...changes } : r)));
  }

  cancelFetch(): void {
    this.showFetch.set(false);
    this.suggestions.set([]);
  }

  importSuggestions(): void {
    const companyId = this.auth.currentCompanyId();
    const chosen = this.selectedSuggestions();
    if (!companyId || chosen.length === 0) return;

    const rows: SaveHolidayDto[] = chosen.map(s => ({
      companyId,
      date: s.date,
      name: s.name,
      type: s.type,
      departmentId: null,
    }));

    this.importing.set(true);
    this.error.set(null);

    this.holidays.import(rows).subscribe({
      next: result => {
        this.importing.set(false);
        this.notice.set(`Added ${result.added}, skipped ${result.skipped}.`);
        this.cancelFetch();
        this.load();
      },
      error: err => {
        this.importing.set(false);
        this.error.set(err?.error?.message ?? 'Import failed.');
      },
    });
  }

  /** Regular holidays carry the premium, so they are the ones worth marking. */
  severityFor(type: HolidayType): 'success' | 'info' | 'secondary' {
    return type === 'Regular' ? 'success' : type === 'SpecialNonWorking' ? 'info' : 'secondary';
  }

  weekdayOf(date: string): string {
    // Parsed as parts rather than through Date(string) so the browser's time zone cannot
    // shift the day.
    const [y, m, d] = date.split('-').map(Number);
    return new Date(y, m - 1, d).toLocaleDateString(undefined, { weekday: 'short' });
  }
}
