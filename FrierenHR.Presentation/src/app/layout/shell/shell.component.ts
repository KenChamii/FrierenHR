import { Component, OnInit, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../core/services/auth.service';
import { MessagingService } from '../../core/services/messaging.service';
import { SetupService } from '../../core/services/setup.service';
import { SetupBannerComponent } from '../setup-banner/setup-banner.component';

interface NavItem { label: string; icon: string; path: string; roles?: string[]; }

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ButtonModule,
    SetupBannerComponent,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent implements OnInit {
  private readonly messaging = inject(MessagingService);
  private readonly setup = inject(SetupService);

  /** The nav item the setup checklist is pointing at, or null when setup is done. */
  readonly setupTarget = computed(() => this.setup.nextRoute());

  /** Conversations with something unread, shown on the Messages link from any screen. */
  readonly unreadMessages = computed(() => this.messaging.unreadCount());

  readonly navItems: NavItem[] = [
    { label: 'Dashboard', icon: 'pi pi-th-large', path: '/dashboard' },
    { label: 'Employees', icon: 'pi pi-users', path: '/employees' },
    { label: 'Departments', icon: 'pi pi-building', path: '/departments', roles: ['HRAdmin'] },
    { label: 'Leave', icon: 'pi pi-calendar', path: '/leave' },
    { label: 'Attendance', icon: 'pi pi-clock', path: '/attendance' },
    { label: 'Corrections', icon: 'pi pi-pencil', path: '/corrections' },
    { label: 'Approvals', icon: 'pi pi-check-square', path: '/approvals' },
    { label: 'My Team', icon: 'pi pi-sitemap', path: '/my-team', roles: ['Manager', 'HRAdmin'] },
    { label: 'Messages', icon: 'pi pi-comments', path: '/messages' },
    { label: 'Rules Config', icon: 'pi pi-sliders-h', path: '/rules-config', roles: ['HRAdmin'] },
    { label: 'Holidays', icon: 'pi pi-calendar-times', path: '/holidays', roles: ['HRAdmin'] },
    { label: 'Shifts', icon: 'pi pi-stopwatch', path: '/shifts', roles: ['HRAdmin'] },
    { label: 'Audit Trail', icon: 'pi pi-history', path: '/audit', roles: ['HRAdmin'] },
    { label: 'Payslips', icon: 'pi pi-money-bill', path: '/payslips' },
    { label: 'Deductions', icon: 'pi pi-minus-circle', path: '/payroll-deductions', roles: ['HRAdmin'] },
    { label: 'Who is Out', icon: 'pi pi-user-minus', path: '/reports/availability', roles: ['Manager', 'HRAdmin'] },
    { label: 'My Forecast', icon: 'pi pi-chart-line', path: '/reports/leave-forecast' },
    { label: 'Payroll Period', icon: 'pi pi-file-export', path: '/reports/payroll-period', roles: ['HRAdmin'] },
    { label: 'Leave Liability', icon: 'pi pi-wallet', path: '/reports/leave-liability', roles: ['HRAdmin'] },
  ];

  constructor(public authService: AuthService) {}

  async ngOnInit(): Promise<void> {
    // Setup progress drives the pointer beside the nav item that comes next.
    this.setup.refresh();

    // Connected here rather than on the Messages screen, so a message that arrives while
    // somebody is on Leave or Attendance still lights up the sidebar.
    try {
      await this.messaging.connect();
      const employeeId = this.authService.currentEmployeeId();
      if (employeeId) {
        this.messaging.getConversations(employeeId)
          .subscribe({ next: list => this.messaging.seedUnread(list) });
      }
    } catch {
      // Chat is not essential to the rest of the application. The Messages screen reports
      // the problem when somebody actually goes there.
    }
  }

  /** True for the Messages link while anything is unread. */
  showsUnread(item: NavItem): boolean {
    return item.path === '/messages' && this.unreadMessages() > 0;
  }

  /**
   * True for the one nav item setup is asking for next, so the sidebar points at the same
   * step the checklist does rather than leaving somebody to find it among nineteen links.
   */
  isSetupTarget(item: NavItem): boolean {
    return this.setupTarget() === item.path;
  }

  readonly initials = computed(() => {
    const name = this.authService.currentFullName();
    if (!name) return '?';
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase())
      .join('');
  });

  visible(item: NavItem): boolean {
    return !item.roles || item.roles.includes(this.authService.currentRole() ?? '');
  }

  async logout(): Promise<void> {
    // Dropped before the token goes, so the next person to sign in on this browser does not
    // inherit a socket authenticated as the previous one.
    await this.messaging.disconnect();
    this.authService.logout();
  }
}