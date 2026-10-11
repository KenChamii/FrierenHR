# Getting started

Setting FrierenHR up in the right order, and the handful of things that catch people out.

The order matters more than it looks. Several screens read what earlier ones wrote, so doing them out of sequence produces an application that appears broken when it is only empty.

## Just trying it: ten minutes

If you want to see the thing work rather than configure it properly:

1. **Create a company** from the landing page. You become its HR administrator and are signed in straight away.
2. **Add one employee** with a monthly rate. A rate is what makes payslips possible later.
3. **Log their hours.** Attendance → This Week, fill Monday, press *Copy across*, save.
4. **Submit the week** from the panel on the left. That locks it, which is the point. Approve it from Approvals.
5. **Run payroll.** Payslips → pick the period → Generate. Open the result.

That exercises the whole spine: hours, the week lock, approval, and money out the other end.

A new company starts with a 9-to-6 shift resting weekends, leave accruing 1.25 days a month capped at 15 after six months, single days auto-approved, overtime at 1.25 and 15 minutes of grace. All of it is editable.

## Full setup, in order

### 1. Departments

Do this first. The employee form's department dropdown reads them, and the availability report groups coverage by department. Adding people first means going back to edit them.

### 2. Shifts

A default shift already exists. Change it if your hours are not 9 to 6, and add more if a department works differently.

A shift is not cosmetic. It sets the expected start time that lateness is measured against, and its rest days feed the working calendar. An employee with no shift of their own falls back to their department's, then to the company default.

### 3. Holidays

**This one is quietly load-bearing.** Leave day counting, the payroll export and the availability report all read the working calendar, which is holidays plus the shift's rest days. With an empty calendar, a five-day leave request spanning a public holiday deducts five days instead of four.

The screen takes a pasted list, one per line, so a year's calendar goes in at once:

```
2026-01-01, New Year's Day, Regular
2026-04-02, Maundy Thursday, Regular
2026-08-21, Ninoy Aquino Day, SpecialNonWorking
```

### 4. Rules

Four rules are seeded. Change them on the Rules Config screen rather than in code: accrual rate and cap, which leave auto-approves, the overtime multiplier, and the lateness grace period.

Rules of the same type stack by priority, and the first match wins. That is how an exception for probationary staff sits above the general rule.

`ApprovalEscalation` rules can be configured and are validated, but are **not evaluated** — escalation reads the approval chain's own setting instead. The screen marks them as not wired.

### 5. Approval chains

Only needed if leave should pass through more than one person. Without a chain, a request that needs approval goes to the employee's manager and stops there.

### 6. Deductions

Only needed before you issue payslips. There are no built-in statutory tables: contributions differ by country and change by circular, so you enter the lines that apply as a fixed amount or a percentage of gross. With none configured, net pay equals gross.

### 7. Employees

Now the dropdowns have something in them. For each person set a department, manager, role, hire date, a temporary password, and a monthly rate with standard monthly hours.

**The rate is optional but consequential.** Someone without one works normally, but payroll skips them, naming them in the run's "left out" list rather than issuing a payslip saying they earned nothing.

## Day to day

**An employee** logs hours under Attendance → This Week, submits at the end of the week, and requests leave as needed. My Forecast shows what their balance becomes and how much the cap will forfeit if they do not book it.

**A manager** works from Approvals: leave requests, timesheets and attendance corrections. Who Is Out shows the next fortnight and flags the days thinnest on cover.

**HR, at month end,** approves outstanding timesheets, checks Payroll Period and exports the CSV, then generates payslips for the same period. Leave Liability gives finance the accrued figure, taking the daily rate as a parameter.

## Things that will catch you out

**Passwords are handed over out of band.** HR sets a temporary password and has to tell the person some other way. There is no invitation email, no forced change on first login, and no reset. Tell people to change it from their Profile page, and do not reuse anything.

**Submitting a week locks its attendance.** That is deliberate: it stops a week being edited after an approver has seen it. It is not a dead end. An employee can unlock their own submission while it is still pending, and HR can reopen one that has been approved. Either way the week returns to draft and needs approving again.

**The punch clock and manual entry do not mix on one day.** A day recorded by clocking in is read-only in the week grid, and vice versa. Delete the one you do not want first.

**Leave and attendance both depend on the working calendar.** If leave counts look wrong, check Holidays and the employee's shift before anything else.

**Payslips keep the figures they were issued with.** Correcting an attendance log afterwards does not rewrite an existing payslip. Regenerate the period if you want it repriced, which replaces that period's payslips rather than adding a second set.
