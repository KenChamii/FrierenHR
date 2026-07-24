import { Component, ElementRef, OnInit, ViewChild, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { FormsModule } from '@angular/forms';
import { AttendanceService } from '../../../../core/services/attendance.service';
import { AuthService } from '../../../../core/services/auth.service';
import { AttendanceRefreshService } from '../../../../core/services/attendance-refresh.service';

type Handle = 'start' | 'end';

// Remote-friendly alternative to punch clocking: a 24-hour rotating dial.
// Drag the green handle to set start time and the red handle to set end
// time; break length is a separate field since it doesn't need its own
// handle on the dial. Backed by POST /api/attendance/log-shift and
// DELETE /api/attendance/{id}.
@Component({
  selector: 'app-shift-clock-widget',
  standalone: true,
  imports: [CommonModule, CardModule, ButtonModule, InputNumberModule, FormsModule],
  templateUrl: './shift-clock-widget.component.html',
  styleUrl: './shift-clock-widget.component.scss',
})
export class ShiftClockWidgetComponent implements OnInit {
  @ViewChild('dial') dialRef!: ElementRef<SVGSVGElement>;

  readonly radius = 100;
  readonly center = 120;

  // Minutes since midnight (0-1439), snapped to 15-minute steps.
  readonly startMinutes = signal(9 * 60);
  readonly endMinutes = signal(18 * 60);
  readonly breakMinutes = signal(60);

  readonly existingLogId = signal<string | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly deleting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  private draggingHandle: Handle | null = null;

  readonly startLabel = computed(() => this.formatMinutes(this.startMinutes()));
  readonly endLabel = computed(() => this.formatMinutes(this.endMinutes()));
  readonly workedHoursLabel = computed(() => {
    let span = this.endMinutes() - this.startMinutes();
    if (span <= 0) span += 24 * 60; // overnight shift
    const worked = Math.max(0, span - this.breakMinutes());
    return (worked / 60).toFixed(1);
  });

  readonly startPoint = computed(() => this.pointFor(this.startMinutes()));
  readonly endPoint = computed(() => this.pointFor(this.endMinutes()));
  readonly arcPath = computed(() => this.describeArc(this.startMinutes(), this.endMinutes()));

  constructor(
    private attendanceService: AttendanceService,
    private authService: AuthService,
    private refreshService: AttendanceRefreshService,
  ) {}

  ngOnInit(): void { this.refresh(); }

  refresh(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) { this.loading.set(false); return; }
    const today = new Date().toISOString().slice(0, 10);
    this.loading.set(true);
    this.attendanceService.getByEmployee(employeeId, today).subscribe({
      next: (logs) => {
        const manual = logs.find(l => l.source === 'Manual') ?? null;
        if (manual) {
          this.existingLogId.set(manual.id);
          this.startMinutes.set(this.minutesFromIso(manual.timeIn));
          if (manual.timeOut) this.endMinutes.set(this.minutesFromIso(manual.timeOut));
          this.breakMinutes.set(manual.breakMinutes ?? 0);
        } else {
          this.existingLogId.set(null);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  // --- Dial interaction ---

  onHandlePointerDown(handle: Handle, event: PointerEvent): void {
    event.preventDefault();
    this.draggingHandle = handle;
    (event.target as Element).setPointerCapture?.(event.pointerId);
  }

  onDialPointerMove(event: PointerEvent): void {
    if (!this.draggingHandle) return;
    const minutes = this.minutesFromPointer(event);
    if (minutes === null) return;
    if (this.draggingHandle === 'start') this.startMinutes.set(minutes);
    else this.endMinutes.set(minutes);
  }

  onDialPointerUp(): void {
    this.draggingHandle = null;
  }

  private minutesFromPointer(event: PointerEvent): number | null {
    const svg = this.dialRef?.nativeElement;
    if (!svg) return null;
    const rect = svg.getBoundingClientRect();
    // viewBox is 240x240; scale client coords into SVG user units.
    const scaleX = 240 / rect.width;
    const scaleY = 240 / rect.height;
    const x = (event.clientX - rect.left) * scaleX - this.center;
    const y = (event.clientY - rect.top) * scaleY - this.center;
    let angleDeg = Math.atan2(x, -y) * (180 / Math.PI);
    if (angleDeg < 0) angleDeg += 360;
    const rawMinutes = (angleDeg / 360) * 24 * 60;
    const snapped = Math.round(rawMinutes / 15) * 15;
    return snapped % (24 * 60);
  }

  private pointFor(minutes: number): { x: number; y: number } {
    const angleRad = (minutes / (24 * 60)) * 2 * Math.PI;
    const x = this.center + this.radius * Math.sin(angleRad);
    const y = this.center - this.radius * Math.cos(angleRad);
    return { x, y };
  }

  private describeArc(startMin: number, endMin: number): string {
    let span = endMin - startMin;
    if (span <= 0) span += 24 * 60;
    const start = this.pointFor(startMin);
    const end = this.pointFor(endMin);
    const largeArc = span > 12 * 60 ? 1 : 0;
    return `M ${this.center} ${this.center} L ${start.x} ${start.y} A ${this.radius} ${this.radius} 0 ${largeArc} 1 ${end.x} ${end.y} Z`;
  }

  private formatMinutes(total: number): string {
    const h = Math.floor(total / 60) % 24;
    const m = total % 60;
    const period = h < 12 ? 'AM' : 'PM';
    const h12 = h % 12 === 0 ? 12 : h % 12;
    return `${h12}:${m.toString().padStart(2, '0')} ${period}`;
  }

  // Used by the template to position the hour-tick labels around the dial.
  sinDeg(deg: number): number {
    return Math.sin((deg * Math.PI) / 180);
  }

  cosDeg(deg: number): number {
    return Math.cos((deg * Math.PI) / 180);
  }

  private minutesFromIso(iso: string): number {
    const d = new Date(iso);
    return d.getHours() * 60 + d.getMinutes();
  }

  private toTimeSpanString(totalMinutes: number): string {
    const h = Math.floor(totalMinutes / 60).toString().padStart(2, '0');
    const m = (totalMinutes % 60).toString().padStart(2, '0');
    return `${h}:${m}:00`;
  }

  // --- Save / delete ---

  submit(): void {
    const employeeId = this.authService.currentEmployeeId();
    if (!employeeId) return;
    this.saving.set(true);
    this.errorMessage.set(null);
    this.attendanceService.logShift({
      employeeId,
      date: new Date().toISOString(),
      startTime: this.toTimeSpanString(this.startMinutes()),
      endTime: this.toTimeSpanString(this.endMinutes()),
      breakMinutes: this.breakMinutes(),
    }).subscribe({
      next: () => { this.saving.set(false); this.refresh(); this.refreshService.notifyChanged(); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err?.error?.message ?? 'Could not save shift.'); },
    });
  }

  deleteToday(): void {
    const id = this.existingLogId();
    if (!id) return;
    this.deleting.set(true);
    this.errorMessage.set(null);
    this.attendanceService.deleteLog(id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.existingLogId.set(null);
        this.startMinutes.set(9 * 60);
        this.endMinutes.set(18 * 60);
        this.breakMinutes.set(60);
        this.refreshService.notifyChanged();
      },
      error: (err) => { this.deleting.set(false); this.errorMessage.set(err?.error?.message ?? 'Could not delete entry.'); },
    });
  }
}
