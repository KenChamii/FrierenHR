import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'employees', loadComponent: () => import('./features/employees/employee-list/employee-list.component').then(m => m.EmployeeListComponent) },
      { path: 'leave', loadComponent: () => import('./features/leave/leave-home/leave-home.component').then(m => m.LeaveHomeComponent) },
      { path: 'attendance', loadComponent: () => import('./features/attendance/attendance-home/attendance-home.component').then(m => m.AttendanceHomeComponent) },
      { path: 'approvals', loadComponent: () => import('./features/approval/approval-home/approval-home.component').then(m => m.ApprovalHomeComponent) },
      { path: 'messages', loadComponent: () => import('./features/messaging/chat-ui/chat-ui.component').then(m => m.ChatUiComponent) },
      {
        path: 'rules-config',
        loadComponent: () => import('./features/company/rule-config/rule-config.component').then(m => m.RuleConfigComponent),
        canActivate: [roleGuard],
        data: { roles: ['HRAdmin'] },
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];