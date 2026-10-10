import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { AuditService } from '../../../core/services/scheduling.service';
import { AuditAction, AuditEntryDto } from '../../../core/models/scheduling.model';

/** One property that changed, flattened out of the entry's JSON for display. */
interface Change { field: string; from: string; to: string; }

interface Row extends AuditEntryDto {
  changeList: Change[];
  expanded: boolean;
}

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, DropdownModule, MessageModule, TagModule],
  templateUrl: './audit.component.html',
  styleUrl: './audit.component.scss',
})
export class AuditComponent implements OnInit {
  private readonly audit = inject(AuditService);

  readonly rows = signal<Row[]>([]);
  readonly total = signal(0);
  readonly skip = signal(0);
  readonly take = 50;
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly entityName = signal<string | null>(null);

  readonly showing = computed(() => {
    const from = this.total() === 0 ? 0 : this.skip() + 1;
    return { from, to: Math.min(this.skip() + this.take, this.total()) };
  });

  readonly hasPrevious = computed(() => this.skip() > 0);
  readonly hasNext = computed(() => this.skip() + this.take < this.total());

  /**
   * The record types worth filtering by. Hardcoded rather than derived from the data, because
   * a list built from whatever happens to be in the trail changes shape as people use it.
   */
  readonly entityOptions = [
    { label: 'Everything', value: null as string | null },
    { label: 'Rules', value: 'CompanyRuleConfig' },
    { label: 'Employees', value: 'Employee' },
    { label: 'Leave requests', value: 'LeaveRequest' },
    { label: 'Leave balances', value: 'LeaveBalance' },
    { label: 'Attendance', value: 'AttendanceLog' },
    { label: 'Corrections', value: 'AttendanceCorrection' },
    { label: 'Timesheets', value: 'Timesheet' },
    { label: 'Holidays', value: 'Holiday' },
    { label: 'Shifts', value: 'Shift' },
    { label: 'Approval chains', value: 'ApprovalChain' },
  ];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.audit.getPage({
      entityName: this.entityName() ?? undefined,
      skip: this.skip(),
      take: this.take,
    }).subscribe({
      next: page => {
        this.rows.set(page.entries.map(entry => ({
          ...entry,
          changeList: this.parse(entry),
          expanded: false,
        })));
        this.total.set(page.total);
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.error.set('Could not load the audit trail.'); },
    });
  }

  onFilterChange(value: string | null): void {
    this.entityName.set(value);
    this.skip.set(0);
    this.load();
  }

  previous(): void { this.skip.update(s => Math.max(0, s - this.take)); this.load(); }
  next(): void { this.skip.update(s => s + this.take); this.load(); }

  toggle(row: Row): void {
    this.rows.update(rows => rows.map(r => r.id === row.id ? { ...r, expanded: !r.expanded } : r));
  }

  severityFor(action: AuditAction): 'success' | 'info' | 'danger' {
    return action === 'Created' ? 'success' : action === 'Updated' ? 'info' : 'danger';
  }

  /** Friendly name for a CLR type name, so the table does not read like a schema. */
  labelFor(entityName: string): string {
    return this.entityOptions.find(o => o.value === entityName)?.label
      ?? entityName.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  /**
   * The stored JSON is { field: { from, to } } for an update and { field: value } otherwise,
   * because an insert or a delete has only one side. Both shapes are flattened here.
   */
  private parse(entry: AuditEntryDto): Change[] {
    let parsed: Record<string, unknown>;
    try { parsed = JSON.parse(entry.changes ?? '{}'); }
    catch { return []; }

    return Object.entries(parsed).map(([field, value]) => {
      if (value !== null && typeof value === 'object' && 'to' in (value as object)) {
        const pair = value as { from?: unknown; to?: unknown };
        return { field, from: this.show(pair.from), to: this.show(pair.to) };
      }
      return entry.action === 'Deleted'
        ? { field, from: this.show(value), to: '' }
        : { field, from: '', to: this.show(value) };
    });
  }

  private show(value: unknown): string {
    if (value === null || value === undefined) return 'empty';
    if (typeof value === 'string' && value.length === 0) return 'empty';
    if (typeof value === 'object') return JSON.stringify(value);
    return String(value);
  }
}
