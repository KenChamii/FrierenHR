import { Component, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../core/services/auth.service';

interface NavItem { label: string; icon: string; path: string; roles?: string[]; }

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ButtonModule],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
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
  ];

  constructor(public authService: AuthService) {}

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

  logout(): void { this.authService.logout(); }
}