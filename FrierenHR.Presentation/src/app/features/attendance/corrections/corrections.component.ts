import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { AttendanceCorrectionService } from '../../../core/services/scheduling.service';
import { AuthService } from '../../../core/services/auth.service';
import { AttendanceCorrectionDto, CorrectionStatus } from '../../../core/models/scheduling.model';
import { todayIso } from '../../../core/utils/dates';

@Component({
  selector: 'app-corrections',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule, ButtonModule, InputNumberModule,
    InputTextModule, MessageModule, TableModule, TagModule, TextareaModule,
  ],
  templateUrl: './corrections.component.html',
  styleUrl: './corrections.component.scss',
})
export class CorrectionsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly corrections = inject(AttendanceCorrectionService);
  private readonly auth = inject(AuthService);

  readonly mine = signal<AttendanceCorrectionDto[]>([]);
  readonly inbox = signal<AttendanceCorrectionDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly deciding = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  /** Only managers and HR see the inbox, matching the endpoint's own role check. */
  readonly canApprove = computed(() => ['Manager', 'HRAdmin'].includes(this.auth.currentRole() ?? ''));

  /** A correction cannot be for a future date, so the picker is capped at today. */
  readonly today = todayIso();

  /** Typed per row, so declining with a reason does not need a dialog. */
  comments: Record<string, string> = {};

  form = this.fb.group({
    date: this.fb.nonNullable.control('', Validators.required),
    requestedTimeIn: this.fb.nonNullable.control('08:00', Validators.required),
    requestedTimeOut: this.fb.nonNullable.control('17:00', Validators.required),
    requestedBreakMinutes: this.fb.nonNullable.control(60),
    reason: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(500)]),
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.corrections.mine().subscribe({
      next: list => { this.mine.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); this.error.set('Could not load your requests.'); },
    });

    if (this.canApprove()) {
      this.corrections.pending().subscribe({
        next: list => this.inbox.set(list),
        error: () => this.inbox.set([]),
      });
    }
  }

  submit(): void {
    const employeeId = this.auth.currentEmployeeId();
    if (!employeeId || this.form.invalid) { this.form.markAllAsTouched(); return; }

    const raw = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.notice.set(null);

    this.corrections.request({
      employeeId,
      date: raw.date,
      requestedTimeIn: `${raw.requestedTimeIn}:00`,
      requestedTimeOut: `${raw.requestedTimeOut}:00`,
      requestedBreakMinutes: raw.requestedBreakMinutes,
      reason: raw.reason,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.notice.set('Sent to your manager.');
        this.form.reset({
          date: '', requestedTimeIn: '08:00', requestedTimeOut: '17:00',
          requestedBreakMinutes: 60, reason: '',
        });
        this.load();
      },
      error: err => { this.saving.set(false); this.error.set(err?.error?.message ?? 'Could not send the request.'); },
    });
  }

  decide(correction: AttendanceCorrectionDto, approve: boolean): void {
    const approverId = this.auth.currentEmployeeId();
    if (!approverId) return;

    this.deciding.set(correction.id);
    this.error.set(null);

    this.corrections.decide(correction.id, {
      decidedByEmployeeId: approverId,
      approve,
      comment: this.comments[correction.id]?.trim() || null,
    }).subscribe({
      next: () => {
        this.deciding.set(null);
        delete this.comments[correction.id];
        this.notice.set(approve
          ? `Approved. ${correction.employeeName}'s attendance for ${correction.date} has been updated.`
          : `Declined ${correction.employeeName}'s request for ${correction.date}.`);
        this.load();
      },
      error: err => { this.deciding.set(null); this.error.set(err?.error?.message ?? 'Could not record the decision.'); },
    });
  }

  severityFor(status: CorrectionStatus): 'success' | 'danger' | 'warn' | 'secondary' {
    return status === 'Approved' ? 'success'
      : status === 'Rejected' ? 'danger'
      : status === 'Pending' ? 'warn'
      : 'secondary';
  }

  /** Hours the request claims, so an approver sees the effect before deciding. */
  claimedHours(correction: AttendanceCorrectionDto): string {
    const start = this.minutesOf(correction.requestedTimeIn);
    const end = this.minutesOf(correction.requestedTimeOut);
    const span = end > start ? end - start : 24 * 60 - (start - end);
    return ((span - correction.requestedBreakMinutes) / 60).toFixed(2);
  }

  private minutesOf(time: string): number {
    const [h, m] = time.split(':').map(Number);
    return h * 60 + m;
  }
}
