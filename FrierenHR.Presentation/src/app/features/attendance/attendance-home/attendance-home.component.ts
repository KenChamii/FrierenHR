import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ClockInWidgetComponent } from './clock-in-widget/clock-in-widget.component';
import { AttendanceHistoryComponent } from './attendance-history/attendance-history.component';

@Component({
  selector: 'app-attendance-home',
  standalone: true,
  imports: [CommonModule, ClockInWidgetComponent, AttendanceHistoryComponent],
  templateUrl: './attendance-home.component.html',
  styleUrl: './attendance-home.component.scss',
})
export class AttendanceHomeComponent {}