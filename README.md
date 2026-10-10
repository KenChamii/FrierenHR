# FrierenHR

A multi-tenant HR system where company policy is configuration rather than code. Leave accrual, overtime multipliers, late grace periods and approval escalation are all defined per company through a rules engine, then applied consistently across leave, attendance, timesheets and approvals.

Built with .NET 10 and Angular 18.

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

## Modules

| Module | What it covers |
| --- | --- |
| Leave | Balances per type, scheduled accrual, requests routed through approval chains |
| Attendance | Clock in and out with break minutes, late grace applied from the company's rule |
| Timesheets | Weekly hours from attendance, submitted once, overtime at the configured multiplier |
| Approvals | Chains defined per company, automatic escalation when a level sits too long |
| Messaging | Direct and group conversations over SignalR, with attachment type and size limits from config |
| People | Employees, departments and reporting lines, scoped so one company never sees another's records |

## Architecture

```
FrierenHR.Core            Entities, enums, rules engine. No database, no HTTP, no framework
FrierenHR.Application     Feature services and their contracts, one folder per capability
FrierenHR.Infrastructure  EF Core mappings, migrations, repositories over SQL Server
FrierenHR.WebAPI          Controllers, JWT auth, SignalR hub, nightly accrual job
FrierenHR.Presentation    Angular app, one feature area per module
FrierenHR.Tests           xUnit, currently covering the rules engine and password hashing
```

Dependencies point inward. `Core` references nothing, which is why the rules engine can be tested without a database or a web host. See `docs/adr/` for the reasoning behind the main structural choices.

## Running it locally

Requires the .NET 10 SDK, Node 20 or later, and SQL Server (LocalDB is fine).

Secrets are not committed. Set them with the user secrets manager:

```bash
cd FrierenHR.WebAPI
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\MSSQLLocalDB;Database=FrierenHRDb;Trusted_Connection=True;"
dotnet user-secrets set "Jwt:Key" "<a random string of at least 32 characters>"
```

Startup fails with a named error if either is missing, rather than failing obscurely later.

Then:

```bash
dotnet run --project FrierenHR.WebAPI      # migrates and seeds on first run in Development
cd FrierenHR.Presentation && npm ci && npm start
```

The API serves Swagger at `/swagger` in Development and a health check at `/health` in every environment.

In Development the app applies pending migrations and seeds demo data on startup. In any other environment it applies migrations but does not seed. For a controlled deployment, generate a migration bundle instead and run it as a deploy step.

## Testing

```bash
dotnet test
```

The suite covers the rules engine end to end: every supported operator, missing and non-numeric fields, priority ordering across stacked rules, and each action type including the accrual cap. Password hashing and the password policy are covered too.

## Known limitations

Recorded deliberately rather than left to be discovered.

- **No refresh tokens.** Access tokens last eight hours and logout is client-side only, so there is no server-side revocation. Acceptable for an internal tool, not for anything public facing.
- **No holiday calendar.** There is no `Holiday` entity, so a leave request spanning a public holiday still deducts that day, and overtime on a holiday gets the ordinary multiplier. This is the next thing worth building.
- **List endpoints are unpaginated.** `Repository<T>.GetAllAsync` returns whole tables. Fine at seed scale, wrong for employee lists and message history in production.
- **Photos and attachments go to local disk.** `LocalDiskPhotoStorageService` writes under `wwwroot/uploads`, which does not survive a container restart. The interface is there to swap in blob storage.
- **Dashboard figures are approximate.** Three of the four counts derive from one approver-scoped query rather than company-wide ones.

