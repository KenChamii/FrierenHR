import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  // The landing page is the application's front door. pathMatch 'full' keeps it to the root
  // URL only, so the shell below still owns /dashboard, /leave and the rest.
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/landing/landing.component').then(m => m.LandingComponent),
  },
  // Kept so existing links to /welcome still work.
  { path: 'welcome', pathMatch: 'full', redirectTo: '' },
  { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'employees', loadComponent: () => import('./features/employees/employee-list/employee-list.component').then(m => m.EmployeeListComponent) },
      { path: 'leave', loadComponent: () => import('./features/leave/leave-home/leave-home.component').then(m => m.LeaveHomeComponent) },
      { path: 'attendance', loadComponent: () => import('./features/attendance/attendance-home/attendance-home.component').then(m => m.AttendanceHomeComponent) },
      { path: 'approvals', loadComponent: () => import('./features/approval/approval-home/approval-home.component').then(m => m.ApprovalHomeComponent) },
      { path: 'messages', loadComponent: () => import('./features/messaging/chat-ui/chat-ui.component').then(m => m.ChatUiComponent) },
      { path: 'profile', loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent) },
      {
        path: 'my-team',
        loadComponent: () => import('./features/team/my-team.component').then(m => m.MyTeamComponent),
        canActivate: [roleGuard],
        data: { roles: ['Manager', 'HRAdmin'] },
      },
      {
        path: 'rules-config',
        loadComponent: () => import('./features/company/rule-config/rule-config.component').then(m => m.RuleConfigComponent),
        canActivate: [roleGuard],
        data: { roles: ['HRAdmin'] },
      },
      {
        path: 'departments',
        loadComponent: () => import('./features/company/departments/departments.component').then(m => m.DepartmentsComponent),
        canActivate: [roleGuard],
        data: { roles: ['HRAdmin'] },
      },
    ],
  },
  // An unknown URL lands somewhere public rather than bouncing off the auth guard.
  { path: '**', redirectTo: '' },
];
