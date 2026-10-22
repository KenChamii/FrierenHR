# FrierenHR

A multi-tenant HR system where company policy is configuration rather than code. Leave accrual, overtime multipliers, late grace periods and approval escalation are all defined per company through a rules engine, then applied consistently across leave, attendance, timesheets and approvals.

Built with .NET 10, Angular 18 and PostgreSQL.

**[Live demo](https://frierenhr.irreversiblechain.workers.dev)** — sign in as `hr@frierenhr.demo`, or create your own company from the landing page.

The demo runs on free hosting that sleeps when idle, so the first request after a quiet
period takes up to a minute while the API and the database wake. It is seeded with one
company that has staff, three weeks of attendance, leave in several states and holidays,
so the screens have something in them.

## Why the rules engine matters

Every company runs leave and overtime on its own terms. The usual outcome is a codebase full of per-client branches. FrierenHR stores each company's policy as a rule that an HR administrator configures, and the engine evaluates it:

```json
{
  "ruleType": "LeaveAccrual",
  "conditions": [
    { "field": "tenureMonths", "op": ">=", "value": 12 },
    { "field": "employmentType", "op": "==", "value": "Regular" }
  ],
  "action": { "type": "AccrueDays", "amount": 1.25, "cap": 15 }
}
```

Which reads as: when months of service is at least 12 and employment type is Regular, accrue 1.25 days per month, up to a maximum balance of 15 days.

Administrators do not write that JSON. The rules screen builds it from a schema the API publishes (`GET /api/rules-config/schema`), so the form can only offer fields and operators the evaluator actually implements. The JSON is the storage format, not the interface.

Rules of the same type stack by priority. `RuleEvaluator.EvaluateFirstMatch` tries them in priority order and the first match wins, which is how an exception for probationary staff sits above the general rule.

The facts include whether the day is a holiday and which kind, so holiday pay is configuration too: "when overtime hours is at least 1 and holiday type is Regular, pay overtime at 2.0 times the normal rate" needs no code change.

## Modules

| Module | What it covers |
| --- | --- |
| Leave | Balances per type, scheduled accrual, requests routed through approval chains |
| Attendance | A week grid for logging a remote schedule, a punch clock and a shift dial for today |
| Scheduling | Per-company holiday calendar and shift definitions, with rest days feeding leave day counts |
| Corrections | A missed clock-in is a request with a reason, decided by a manager, and approving writes the row |
| Audit | Every change to a record, captured by a save interceptor rather than by each service |
| Timesheets | Weekly hours from attendance, submitted to lock the week, reopenable for changes |
| Payroll | Payslips priced from the hours on record, with deduction lines a company defines |
| Approvals | Chains defined per company, automatic escalation when a level sits too long |
| Messaging | Direct and group conversations over SignalR, with unread badges and attachment limits from config |
| People | Employees, departments and reporting lines, scoped so one company never sees another's records |
| Reports | Who is out, projected balances, a payroll period export and the leave liability figure |
| Onboarding | Self-service sign-up that creates a company, its first administrator and a working configuration |

## Time tracking

Three ways in, because logging a fixed remote schedule and recording a walk-in shift are different jobs:

- **This week.** A Monday to Sunday grid with start, end and break per day. Fill one day, press copy across, and the rest of the working week follows, skipping rest days and holidays. The whole week saves in one transaction, so it is never left half written.
- **Today.** The punch clock, and a 24-hour dial for setting a shift by dragging its start and end.

A day the punch clock already recorded is read-only in the grid rather than silently overwritten, which mirrors the rule the service enforces anyway.

Submitting a week locks its attendance, so it cannot be edited out from under an approver. The lock is not a dead end: an employee can withdraw their own submission while it is still pending, and HR can reopen a week that has already been approved. Either returns it to draft, and resubmitting starts the approval again, so a changed week cannot slip past someone who already looked at it.

## Payslips

Pay is a monthly rate held against the employee, divided by their standard monthly hours to give an hourly rate. A payslip is then regular hours plus payable overtime at that rate, less whatever deduction lines the company has defined.

The hours come from the payroll export rather than a second pass over the attendance logs, so a payslip and the export can never disagree about a period.

Three things worth knowing:

**Nothing statutory is built in.** There are no SSS, PhilHealth, Pag-IBIG or withholding tables in this codebase. They differ by country and change by circular, so a hardcoded table would be confidently wrong within a year. Deductions are named lines an administrator enters as a fixed amount or a percentage of gross. With none configured, net equals gross.

**A payslip keeps the figures it was issued with.** Correcting an attendance log or changing a rate afterwards must not quietly rewrite what somebody was paid, so the hours, the rate and every line are stored rather than recomputed on each view. Re-running a period replaces that period's payslips, enforced by a unique index.

**An employee with no rate is skipped by name.** The run returns a list of who was left out and why, rather than issuing a payslip saying they earned nothing.

## Reports

Four read-only views, all derived on request rather than stored, so a corrected attendance log or an approved leave request changes them with nothing to recalculate.

| Report | Question it answers | Who can see it |
| --- | --- | --- |
| Who is out | Who is unavailable over the next fortnight, and which days are thinnest on cover | Manager, HRAdmin |
| Projected balance | How many leave days somebody will have by a chosen month, and how many the cap will forfeit | Your own; HR can see anyone's |
| Payroll period | Hours, overtime, minutes late after grace and leave taken, per employee, exportable as CSV | HRAdmin |
| Leave liability | What unused leave would cost if it were paid out, by employee and by department | HRAdmin |

Two decisions worth knowing about:

The availability grid reads each employee's own calendar, so a rest day and a public holiday appear as themselves rather than as absence, and coverage percentages ignore days nobody was expected in. A closed office is not a staffing crisis.

The liability report takes the daily rate as a parameter rather than reading one from configuration. No salary is held against an employee, and the global config row is not company-scoped, so a rate taken from either would be quietly wrong for every tenant but one.

## Getting an account

Sign-up creates a company and its first HR administrator together, in one transaction, from the landing page. The two cannot be separated: every other route to creating an employee requires an HRAdmin token for that company, so a company committed on its own would be a tenant nobody could ever log into.

A new company starts with a working configuration rather than an empty one, since nothing knows the working day without a shift and every request needs a manual decision without rules:

- A 9 to 6 default shift with a one-hour break, resting weekends
- Leave accruing 1.25 days a month after six months, capped at 15
- Single days auto-approved, anything longer routed to a manager
- Overtime at 1.25, with 15 minutes of grace on lateness

All of it is editable afterwards on the Shifts and Rules screens.

The order the rest of the setup is done in matters, because several screens read what earlier ones wrote. See [docs/getting-started.md](docs/getting-started.md) for that order, and for the handful of things that catch people out.

## Architecture

```
FrierenHR.Core            Entities, enums, rules engine. No database, no HTTP, no framework
FrierenHR.Application     Feature services and their contracts, one folder per capability
FrierenHR.Infrastructure  EF Core mappings, migrations, repositories over PostgreSQL
FrierenHR.WebAPI          Controllers, JWT auth, SignalR hub, nightly accrual job
FrierenHR.Presentation    Angular app, one feature area per module
FrierenHR.Tests           xUnit, covering the rules engine, the working calendar, leave
                          projection, payslip arithmetic, CSV output and password hashing
```

Dependencies point inward. `Core` references nothing, which is why the rules engine can be tested without a database or a web host. See `docs/adr/` for the reasoning behind the main structural choices.

## Running it locally

Requires the .NET 10 SDK, Node 20 or later, and Docker.

Start the database first. It binds host port 5433 rather than 5432, so it does not collide
with a PostgreSQL installed natively:

```bash
docker compose -f docker-compose.dev.yml up -d
```

Secrets are not committed. Set them with the user secrets manager:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=frierenhr;Username=frierenhr;Password=localdev" --project FrierenHR.WebAPI
dotnet user-secrets set "Jwt:Key" "<a random string of at least 32 characters>" --project FrierenHR.WebAPI
```

Startup fails with a named error if either is missing, rather than failing obscurely later.

Then:

```bash
dotnet run --project FrierenHR.WebAPI      # migrates and seeds on first run in Development
cd FrierenHR.Presentation && npm ci && npm start
```

The API serves Swagger at `/swagger` in Development and a health check at `/health` in every environment.

In Development the app applies pending migrations and seeds demo data on startup. In any other environment it applies migrations but does not seed. For a controlled deployment, generate a migration bundle instead and run it as a deploy step.

## Deployment

Everything runs in containers, and both images are built on every push so a broken
Dockerfile fails in CI rather than at deploy time.

```bash
cp .env.example .env     # fill in POSTGRES_PASSWORD and JWT_KEY
docker compose up --build
```

That brings up PostgreSQL, the API, and nginx serving the built frontend and proxying
`/api`, `/hubs` and `/uploads` to the API, so the browser sees one origin and CORS does not
apply. Open `http://localhost:8080`.

The hosted demo is arranged differently, because free hosting gives each service its own
domain: the API on Render, the database on Neon, and the frontend on Cloudflare Workers.
There the frontend holds the API's absolute URL and the API allows that origin explicitly.
`FrierenHR.Presentation/src/environments/environment.ts` says which arrangement is in use.

Migrations are applied on startup. Demo data is seeded only when `Demo__Seed` is set, and
the seeder refuses any database that already holds a company.

## Testing

```bash
dotnet test
```

The suite covers the pieces that hold real logic and no dependencies:

- **Rules engine**, end to end: every supported operator, missing and non-numeric fields, priority ordering across stacked rules, and each action type including the accrual cap.
- **Working calendar**: working-day counts across weekends and holidays, duplicate dates, and inverted ranges.
- **Leave projection**: the cap biting part way through a month, a balance already above a lowered cap, and the month forfeiture first begins.
- **Payslip arithmetic**: overtime priced at the plain rate because it arrives pre-multiplied, percentages all taken on gross so their order cannot change net pay, and deductions exceeding gross leaving zero rather than a negative payslip.
- **CSV output**: quoting a name containing a comma, a decimal under a comma locale, and the byte order mark that keeps accented names readable in Excel.
- **Password hashing** and the password policy.


