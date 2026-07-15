import { Component } from '@angular/core';
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
    { label: 'Leave', icon: 'pi pi-calendar', path: '/leave' },
    { label: 'Attendance', icon: 'pi pi-clock', path: '/attendance' },
    { label: 'Approvals', icon: 'pi pi-check-square', path: '/approvals' },
    { label: 'Messages', icon: 'pi pi-comments', path: '/messages' },
    { label: 'Rules Config', icon: 'pi pi-sliders-h', path: '/rules-config', roles: ['HRAdmin'] },
  ];

  constructor(public authService: AuthService) {}

  visible(item: NavItem): boolean {
    return !item.roles || item.roles.includes(this.authService.currentRole() ?? '');
  }

  logout(): void { this.authService.logout(); }
}