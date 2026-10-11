import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../../core/services/auth.service';

interface GuideItem {
  title: string;
  body: string;
  route: string;
  action: string;
  /** Shown only to people who approve things. */
  managersOnly?: boolean;
}

const DISMISSED_KEY = 'hr_employee_guide_dismissed';

/**
 * How the application is used week to week, for somebody who did not set it up.
 *
 * Not a checklist: an employee has no setup to finish, they have a routine to learn, and
 * nothing here is ever "done". So it is orientation that can be dismissed and brought back,
 * rather than progress that gets ticked off.
 */
@Component({
  selector: 'app-employee-guide',
  standalone: true,
  imports: [CommonModule, RouterLink, ButtonModule],
  templateUrl: './employee-guide.component.html',
  styleUrl: './employee-guide.component.scss',
})
export class EmployeeGuideComponent {
  private readonly auth = inject(AuthService);

  readonly dismissed = signal(this.readDismissed());

  private readonly isManager = computed(() =>
    this.auth.currentRole() === 'Manager' || this.auth.currentRole() === 'HRAdmin');

  private readonly allItems: GuideItem[] = [
    {
      title: 'Log your hours',
      body: 'Attendance, This week. Fill in Monday, press Copy across, and the rest of the '
        + 'working week follows. Rest days and holidays are skipped for you.',
      route: '/attendance',
      action: 'Open attendance',
    },
    {
      title: 'Submit the week when it is done',
      body: 'Submitting locks the week so it cannot change after your manager has seen it. '
        + 'If you spot a mistake, unlock it, fix it, and submit again.',
      route: '/attendance',
      action: 'Open attendance',
    },
    {
      title: 'Request leave',
      body: 'A single day usually clears itself; anything longer goes to your manager. The '
        + 'form shows how many days will actually be deducted before you submit.',
      route: '/leave',
      action: 'Request leave',
    },
    {
      title: 'See where your balance is heading',
      body: 'My forecast shows what you will have by a chosen month, and warns you if the '
        + 'cap would turn days away before you get to use them.',
      route: '/reports/leave-forecast',
      action: 'Open my forecast',
    },
    {
      title: 'Fix a day that is already locked',
      body: 'Once a week has been approved it cannot be edited. Raise a correction instead, '
        + 'with the reason, and your manager decides.',
      route: '/corrections',
      action: 'Raise a correction',
    },
    {
      title: 'Decide what is waiting on you',
      body: 'Leave requests, timesheets and corrections from your team all arrive in '
        + 'Approvals. Who is out shows the fortnight ahead and the thinnest days.',
      route: '/approvals',
      action: 'Open approvals',
      managersOnly: true,
    },
    {
      title: 'Find your payslips',
      body: 'They appear here once HR runs payroll for a period you worked. Each one shows '
        + 'the hours and rate behind the figure, so it can be checked rather than trusted.',
      route: '/payslips',
      action: 'Open payslips',
    },
  ];

  readonly items = computed(() =>
    this.allItems.filter(item => !item.managersOnly || this.isManager()));

  show(): void {
    this.dismissed.set(false);
    this.write('false');
  }

  dismiss(): void {
    this.dismissed.set(true);
    this.write('true');
  }

  private readDismissed(): boolean {
    try {
      return localStorage.getItem(DISMISSED_KEY) === 'true';
    } catch {
      return false;
    }
  }

  private write(value: string): void {
    try {
      localStorage.setItem(DISMISSED_KEY, value);
    } catch {
      // Private browsing, or storage refused. It holds for this session, which is enough
      // for something that is only a display preference.
    }
  }
}
