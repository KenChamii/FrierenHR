import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

interface LedgerRow { date: string; entry: string; change: string; balance: string; negative?: boolean; }
interface BalancePoint { month: string; value: number; }
interface Module { index: string; name: string; blurb: string; }
interface ChainStep { level: string; who: string; state: string; done?: boolean; final?: boolean; }
interface Layer { name: string; owns: string; }

/** Public landing page, outside the authenticated shell. */
@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss',
})
export class LandingComponent {
  /** Ceiling from the accrual rule shown in the hero, used to scale the chart. */
  readonly cap = 15;

  readonly balances: BalancePoint[] = [
    { month: 'Apr', value: 6.25 },
    { month: 'May', value: 7.5 },
    { month: 'Jun', value: 8.75 },
    { month: 'Jul', value: 6.75 },
    { month: 'Aug', value: 8.0 },
    { month: 'Sep', value: 9.25 },
  ];

  readonly ledger: LedgerRow[] = [
    { date: '30 Apr', entry: 'Monthly accrual', change: '+1.25', balance: '6.25' },
    { date: '31 May', entry: 'Monthly accrual', change: '+1.25', balance: '7.50' },
    { date: '30 Jun', entry: 'Monthly accrual', change: '+1.25', balance: '8.75' },
    { date: '14 Jul', entry: 'Leave taken, 2 days, approved', change: '−2.00', balance: '6.75', negative: true },
    { date: '31 Jul', entry: 'Monthly accrual', change: '+1.25', balance: '8.00' },
    { date: '31 Aug', entry: 'Monthly accrual', change: '+1.25', balance: '9.25' },
  ];

  readonly modules: Module[] = [
    { index: '01', name: 'Leave', blurb: 'Balances per type, accrual on a schedule, requests that route themselves, and a ledger behind every figure.' },
    { index: '02', name: 'Attendance', blurb: 'Clock in and out with break minutes, late grace applied from the company rule rather than a constant.' },
    { index: '03', name: 'Timesheets', blurb: 'Weekly hours assembled from attendance, submitted once, with overtime multiplied at the configured rate.' },
    { index: '04', name: 'Approval chains', blurb: 'Levels defined per company, escalation when a step sits too long, and an inbox that only shows what is yours.' },
    { index: '05', name: 'Messaging', blurb: 'Conversations between colleagues delivered live, with attachments held to a type and size the company sets.' },
    { index: '06', name: 'People', blurb: 'Employees, departments and reporting lines, scoped so one company never sees another records.' },
  ];

  readonly chain: ChainStep[] = [
    { level: 'Filed', who: 'Employee', state: '2 days, vacation', done: true },
    { level: 'Level 1', who: 'Supervisor', state: 'Approved, 11 Jul', done: true },
    { level: 'Level 2', who: 'Department head', state: 'Waiting, 2 days' },
    { level: 'Level 3', who: 'HR', state: 'Not yet reached', final: true },
  ];

  readonly layers: Layer[] = [
    { name: 'Core', owns: 'Entities, enums and the rules engine. No database, no HTTP, no framework.' },
    { name: 'Application', owns: 'Feature services and the contracts they depend on, one folder per capability.' },
    { name: 'Infrastructure', owns: 'EF Core mappings, migrations and repositories over SQL Server.' },
    { name: 'Web API', owns: 'Controllers, JWT authentication, the messaging hub and the nightly accrual job.' },
    { name: 'Presentation', owns: 'Angular, with guards and a feature area per module.' },
  ];

  readonly ruleTypes = [
    'Leave accrual', 'Leave approval', 'Overtime multiplier', 'Late grace period', 'Approval escalation',
  ];

  /** Bar height as a percentage of the cap. */
  heightFor(value: number): string {
    return `${Math.min(100, (value / this.cap) * 100)}%`;
  }
}
