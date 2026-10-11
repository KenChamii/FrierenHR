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
  // Sign-up creates a company and its first HRAdmin together, so it needs no account.
  { path: 'register', loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent) },
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
      // Anyone can raise a correction; the approver queue inside is role gated by the API.
      { path: 'corrections', loadComponent: () => import('./features/attendance/corrections/corrections.component').then(m => m.CorrectionsComponent) },
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
      {
        path: 'holidays',
        loadComponent: () => import('./features/company/holidays/holidays.component').then(m => m.HolidaysComponent),
        canActivate: [roleGuard],
        data: { roles: ['HRAdmin'] },
      },
      {
        path: 'shifts',
        loadComponent: () => import('./features/company/shifts/shifts.component').then(m => m.ShiftsComponent),
        canActivate: [roleGuard],
        data: { roles: ['HRAdmin'] },
      },
      // Payslips are self-service: the endpoint returns the caller's own unless they are HR.
      {
        path: 'payslips',
        loadComponent: () => import('./features/payroll/payslips/payslips.component').then(m => m.PayslipsComponent),
      },
      {
        path: 'payroll-deductions',
        loadComponent: () => import('./features/payroll/deductions/deductions.component').then(m => m.PayrollDeductionsComponent),
        canActivate: [roleGuard],
        data: { roles: ['HRAdmin'] },
      },
      // Reports. The forecast is a person's own balance, so it needs no role; the other three
      // read across the company and are gated here as well as at the API.
      {
        path: 'reports/leave-forecast',
        loadComponent: () => import('./features/reports/leave-forecast/leave-forecast.component').then(m => m.LeaveForecastComponent),
      },
      {
        path: 'reports/availability',
        loadComponent: () => import('./features/reports/availability/availability.component').then(m => m.AvailabilityComponent),
        canActivate: [roleGuard],
        data: { roles: ['Manager', 'HRAdmin'] },
      },
      {
        path: 'reports/payroll-period',
        loadComponent: () => import('./features/reports/payroll-period/payroll-period.component').then(m => m.PayrollPeriodComponent),
        canActivate: [roleGuard],
        data: { roles: ['HRAdmin'] },
      },
      {
        path: 'reports/leave-liability',
        loadComponent: () => import('./features/reports/leave-liability/leave-liability.component').then(m => m.LeaveLiabilityComponent),
        canActivate: [roleGuard],
        data: { roles: ['HRAdmin'] },
      },
      {
        path: 'audit',
        loadComponent: () => import('./features/company/audit/audit.component').then(m => m.AuditComponent),
        canActivate: [roleGuard],
        data: { roles: ['HRAdmin'] },
      },
    ],
  },
  // An unknown URL lands somewhere public rather than bouncing off the auth guard.
  { path: '**', redirectTo: '' },
];
