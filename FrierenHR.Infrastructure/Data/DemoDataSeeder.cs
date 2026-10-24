using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Security;
using FrierenHR.Application.Features.Onboarding;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Data;

/// <summary>Fills an empty database with one company that has enough history to be worth looking at: staff across departments.</summary>
public static class DemoDataSeeder
{
    public const string CompanyCode = "FRIEREN";
    public const string AdminEmail = "hr@frierenhr.demo";
    public const string EmailDomain = "frierenhr.demo";

    /// <summary>Weeks of attendance history generated behind the current date.</summary>
    private const int HistoryWeeks = 3;

    /// <summary>Fixed so the demo looks the same every time it is rebuilt.</summary>
    private const int RandomSeed = 20261011;

    public static async Task SeedAsync(
        FrierenHRDbContext context,
        IRegistrationService registration,
        string password,
        CancellationToken ct = default)
    {
        // Refuses rather than merges.
        if (await context.Companies.AnyAsync(ct)) return;

        PasswordPolicy.Validate(password);

        var registered = await registration.RegisterCompanyAsync(new RegisterCompanyDto(
            CompanyName: "Frieren Demo Co.",
            CompanyCode: CompanyCode,
            FirstName: "Hanna",
            LastName: "Reyes",
            Email: AdminEmail,
            Password: password), ct);

        await PopulateAsync(context, registered.CompanyId, registered.EmployeeId, password, EmailDomain, ct);
    }

    /// <summary>Adds the demo departments, staff and history to a company that already exists.</summary>
    /// <param name="emailDomain">Emails are unique across every company, so a second company loading this data needs its own domain or it collides with the first.</param>
    public static async Task PopulateAsync(
        FrierenHRDbContext context,
        Guid companyId,
        Guid adminId,
        string password,
        string emailDomain,
        CancellationToken ct = default)
    {
        var domain = emailDomain;

        var defaultShift = await context.Shifts
            .Where(s => s.CompanyId == companyId && s.IsDefault)
            .FirstAsync(ct);

        var nightShift = new Shift
        {
            CompanyId = companyId,
            Name = "Night shift",
            StartTime = new TimeOnly(22, 0),
            EndTime = new TimeOnly(6, 0),
            BreakMinutes = 60,
            RestDays = WeekDays.Sunday,
        };
        context.Shifts.Add(nightShift);

        var engineering = new Department { CompanyId = companyId, Name = "Engineering", ShiftId = defaultShift.Id };
        var support = new Department { CompanyId = companyId, Name = "Customer Support", ShiftId = nightShift.Id };
        var people = new Department { CompanyId = companyId, Name = "People & Culture", ShiftId = defaultShift.Id };
        context.Departments.AddRange(engineering, support, people);
        await context.SaveChangesAsync(ct);

        // The administrator belongs somewhere too, or the org chart has a person floating outside it and the department headcounts do not add up to the staff list.
        var admin = await context.Employees.FirstAsync(e => e.Id == adminId, ct);
        admin.DepartmentId = people.Id;
        admin.MonthlyRate = 95_000m;

        var today = DateTime.UtcNow.Date;

        // The leads report to the administrator, which is what puts their leave requests in the administrator's approvals inbox.
        var engineeringLead = NewEmployee(
            companyId, engineering.Id, "Marco", "Villanueva", $"marco@{domain}",
            EmployeeRole.Manager, 110_000m, today.AddYears(-4), password, adminId);

        var supportLead = NewEmployee(
            companyId, support.Id, "Divina", "Santos", $"divina@{domain}",
            EmployeeRole.Manager, 88_000m, today.AddYears(-3), password, adminId);

        context.Employees.AddRange(engineeringLead, supportLead);
        await context.SaveChangesAsync(ct);

        var staff = new List<Employee>
        {
            NewEmployee(companyId, engineering.Id, "Jun", "Alcantara", $"jun@{domain}",
                EmployeeRole.Employee, 62_000m, today.AddMonths(-20), password, engineeringLead.Id),
            NewEmployee(companyId, engineering.Id, "Precious", "Lim", $"precious@{domain}",
                EmployeeRole.Employee, 58_000m, today.AddMonths(-14), password, engineeringLead.Id),
            NewEmployee(companyId, engineering.Id, "Rafael", "Ocampo", $"rafael@{domain}",
                EmployeeRole.Employee, 45_000m, today.AddMonths(-4), password, engineeringLead.Id),
            NewEmployee(companyId, support.Id, "Mariel", "Bautista", $"mariel@{domain}",
                EmployeeRole.Employee, 38_000m, today.AddMonths(-26), password, supportLead.Id),
            NewEmployee(companyId, support.Id, "Kiko", "Dela Cruz", $"kiko@{domain}",
                EmployeeRole.Employee, 36_000m, today.AddMonths(-9), password, supportLead.Id),

            // Deliberately hired five weeks ago: below the six-month tenure the starter accrual rule requires.
            NewEmployee(companyId, support.Id, "Trina", "Espinosa", $"trina@{domain}",
                EmployeeRole.Employee, 34_000m, today.AddDays(-35), password, supportLead.Id),

            // No recorded pay, which is what makes payslip generation skip somebody rather than price them at zero.
            NewEmployee(companyId, people.Id, "Noel", "Gatchalian", $"noel@{domain}",
                EmployeeRole.Employee, null, today.AddMonths(-7), password, adminId),
        };

        context.Employees.AddRange(staff);
        await context.SaveChangesAsync(ct);

        var everyone = new List<Employee> { admin, engineeringLead, supportLead };
        everyone.AddRange(staff);

        context.LeaveBalances.AddRange(BuildLeaveBalances(everyone, today));
        context.Holidays.AddRange(BuildHolidays(companyId, today));
        var leaveRequests = BuildLeaveRequests(staff, engineeringLead, supportLead, today);
        context.LeaveRequests.AddRange(leaveRequests);

        // Pending requests only show up on the approvals screen when they have a running approval behind them.
        var chain = await context.ApprovalChains
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.CompanyId == companyId, ct);

        if (chain is null)
        {
            chain = new ApprovalChain
            {
                CompanyId = companyId,
                Name = "Standard leave approval",
                Steps =
                [
                    new ApprovalChainStep { StepOrder = 1, ApproverRole = EmployeeRole.Manager, EscalateAfterDays = 3 },
                    new ApprovalChainStep { StepOrder = 2, ApproverRole = EmployeeRole.HRAdmin, EscalateAfterDays = 3 },
                ],
            };
            context.ApprovalChains.Add(chain);
        }

        context.ApprovalInstances.AddRange(leaveRequests
            .Where(r => r.Status == LeaveStatus.Pending)
            .Select(r => new ApprovalInstance
            {
                LeaveRequestId = r.Id,
                ApprovalChainId = chain.Id,
                CurrentStepOrder = chain.Steps.Min(step => step.StepOrder),
                Status = ApprovalStatus.Pending,
                StepStartedAt = r.RequestedAt,
            }));
        context.AttendanceLogs.AddRange(BuildAttendance(everyone, defaultShift, nightShift, support.Id, today));

        await context.SaveChangesAsync(ct);
    }

    private static Employee NewEmployee(
        Guid companyId, Guid departmentId, string firstName, string lastName, string email,
        EmployeeRole role, decimal? monthlyRate, DateTime hireDate, string password,
        Guid? managerId = null) => new()
        {
            CompanyId = companyId,
            DepartmentId = departmentId,
            ManagerId = managerId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = PasswordHasher.Hash(password),
            HireDate = hireDate,
            MonthlyRate = monthlyRate,
            Role = role,
            IsActive = true,

            // False on purpose.
            MustChangePassword = false,
        };

    /// <summary>Balances that look accrued rather than granted: roughly the starter rule's 1.25 days a month, capped at 15.</summary>
    private static List<LeaveBalance> BuildLeaveBalances(List<Employee> employees, DateTime today)
    {
        var balances = new List<LeaveBalance>();

        foreach (var employee in employees)
        {
            var tenure = employee.TenureMonths(today);
            var vacation = tenure >= 6 ? Math.Min(15m, Math.Round(tenure * 1.25m, 2)) : 0m;

            balances.Add(new LeaveBalance
            {
                EmployeeId = employee.Id,
                LeaveType = LeaveType.Vacation,
                Balance = vacation,
                LastAccrualDate = tenure >= 6 ? new DateTime(today.Year, today.Month, 1) : null,
            });

            balances.Add(new LeaveBalance
            {
                EmployeeId = employee.Id,
                LeaveType = LeaveType.Sick,
                Balance = tenure >= 6 ? 10m : 0m,
                LastAccrualDate = tenure >= 6 ? new DateTime(today.Year, today.Month, 1) : null,
            });
        }

        return balances;
    }

    /// <summary>A handful of Philippine holidays on fixed dates, for the current year.</summary>
    private static List<Holiday> BuildHolidays(Guid companyId, DateTime today)
    {
        var year = today.Year;

        return
        [
            new Holiday { CompanyId = companyId, Date = new DateOnly(year, 1, 1), Name = "New Year's Day", Type = HolidayType.Regular },
            new Holiday { CompanyId = companyId, Date = new DateOnly(year, 5, 1), Name = "Labor Day", Type = HolidayType.Regular },
            new Holiday { CompanyId = companyId, Date = new DateOnly(year, 6, 12), Name = "Independence Day", Type = HolidayType.Regular },
            new Holiday { CompanyId = companyId, Date = new DateOnly(year, 8, 21), Name = "Ninoy Aquino Day", Type = HolidayType.SpecialNonWorking },
            new Holiday { CompanyId = companyId, Date = new DateOnly(year, 11, 1), Name = "All Saints' Day", Type = HolidayType.SpecialNonWorking },
            new Holiday { CompanyId = companyId, Date = new DateOnly(year, 12, 25), Name = "Christmas Day", Type = HolidayType.Regular },
            new Holiday { CompanyId = companyId, Date = new DateOnly(year, 12, 30), Name = "Rizal Day", Type = HolidayType.Regular },
        ];
    }

    /// <summary>Leave in every state the approval screens have to render: approved, pending, rejected.</summary>
    private static List<LeaveRequest> BuildLeaveRequests(
        List<Employee> staff, Employee engineeringLead, Employee supportLead, DateTime today)
    {
        var requests = new List<LeaveRequest>();
        if (staff.Count < 5) return requests;

        // Already taken and decided.
        requests.Add(new LeaveRequest
        {
            EmployeeId = staff[0].Id,
            LeaveType = LeaveType.Vacation,
            StartDate = today.AddDays(-12),
            EndDate = today.AddDays(-10),
            Days = 3m,
            Reason = "Family trip",
            Status = LeaveStatus.Approved,
            RequestedAt = today.AddDays(-20),
            DecidedAt = today.AddDays(-18),
            DecidedByEmployeeId = engineeringLead.Id,
            RequiresApproval = true,
        });

        // Waiting on a manager, which is what puts a number on the approvals tile.
        requests.Add(new LeaveRequest
        {
            EmployeeId = engineeringLead.Id,
            LeaveType = LeaveType.Vacation,
            StartDate = today.AddDays(6),
            EndDate = today.AddDays(8),
            Days = 3m,
            Reason = "Wedding out of town",
            Status = LeaveStatus.Pending,
            RequestedAt = today.AddDays(-2),
            RequiresApproval = true,
        });

        requests.Add(new LeaveRequest
        {
            EmployeeId = supportLead.Id,
            LeaveType = LeaveType.Emergency,
            StartDate = today.AddDays(1),
            EndDate = today.AddDays(1),
            Days = 1m,
            Reason = "Burst pipe at home",
            Status = LeaveStatus.Pending,
            RequestedAt = today.AddDays(-1),
            RequiresApproval = true,
        });

        requests.Add(new LeaveRequest
        {
            EmployeeId = staff[4].Id,
            LeaveType = LeaveType.Vacation,
            StartDate = today.AddDays(-4),
            EndDate = today.AddDays(-2),
            Days = 3m,
            Reason = "Short break",
            Status = LeaveStatus.Rejected,
            RequestedAt = today.AddDays(-9),
            DecidedAt = today.AddDays(-7),
            DecidedByEmployeeId = supportLead.Id,
            RequiresApproval = true,
        });

        // A single day, which the starter approval rule clears without a manager.
        requests.Add(new LeaveRequest
        {
            EmployeeId = staff[2].Id,
            LeaveType = LeaveType.Sick,
            StartDate = today.AddDays(-6),
            EndDate = today.AddDays(-6),
            Days = 1m,
            Reason = "Migraine",
            Status = LeaveStatus.Approved,
            RequestedAt = today.AddDays(-6),
            DecidedAt = today.AddDays(-6),
            RequiresApproval = false,
        });

        // Somebody out right now, so the availability view is not empty on the day it is opened.
        requests.Add(new LeaveRequest
        {
            EmployeeId = staff[5].Id,
            LeaveType = LeaveType.Sick,
            StartDate = today,
            EndDate = today,
            Days = 1m,
            Reason = "Fever",
            Status = LeaveStatus.Approved,
            RequestedAt = today,
            DecidedAt = today,
            DecidedByEmployeeId = supportLead.Id,
            RequiresApproval = false,
        });

        return requests;
    }

    /// <summary>Attendance for the past few weeks on each person's working days.</summary>
    private static List<AttendanceLog> BuildAttendance(
        List<Employee> employees, Shift dayShift, Shift nightShift, Guid supportDepartmentId, DateTime today)
    {
        var logs = new List<AttendanceLog>();
        var random = new Random(RandomSeed);

        foreach (var employee in employees)
        {
            var shift = employee.DepartmentId == supportDepartmentId ? nightShift : dayShift;

            for (var dayOffset = HistoryWeeks * 7; dayOffset >= 1; dayOffset--)
            {
                var date = today.AddDays(-dayOffset);
                if (IsRestDay(date.DayOfWeek, shift.RestDays)) continue;
                if (date < employee.HireDate.Date) continue;

                // A couple of absences each, so the reports are not uniformly perfect.
                if (random.Next(0, 16) == 0) continue;

                var lateMinutes = random.Next(0, 10) switch
                {
                    < 6 => 0,                     // on time
                    < 8 => random.Next(1, 15),    // inside the grace period
                    8 => random.Next(16, 45),     // late
                    _ => random.Next(-20, -2),    // early
                };

                var timeIn = date.Add(shift.StartTime.ToTimeSpan()).AddMinutes(lateMinutes);

                var scheduled = shift.CrossesMidnight
                    ? (shift.EndTime.ToTimeSpan() + TimeSpan.FromHours(24)) - shift.StartTime.ToTimeSpan()
                    : shift.EndTime.ToTimeSpan() - shift.StartTime.ToTimeSpan();

                var overtimeMinutes = random.Next(0, 10) < 2 ? random.Next(30, 150) : 0;

                // One open shift per person: a clock-in with no clock-out.
                var leaveOpen = dayOffset == 2;

                logs.Add(new AttendanceLog
                {
                    EmployeeId = employee.Id,
                    TimeIn = timeIn,
                    TimeOut = leaveOpen ? null : timeIn.Add(scheduled).AddMinutes(overtimeMinutes),
                    BreakMinutes = shift.BreakMinutes,
                    Source = random.Next(0, 5) == 0 ? "Mobile" : "Web",
                });
            }
        }

        return logs;
    }

    private static bool IsRestDay(DayOfWeek day, WeekDays restDays)
    {
        var flag = day switch
        {
            DayOfWeek.Monday => WeekDays.Monday,
            DayOfWeek.Tuesday => WeekDays.Tuesday,
            DayOfWeek.Wednesday => WeekDays.Wednesday,
            DayOfWeek.Thursday => WeekDays.Thursday,
            DayOfWeek.Friday => WeekDays.Friday,
            DayOfWeek.Saturday => WeekDays.Saturday,
            _ => WeekDays.Sunday,
        };

        return restDays.HasFlag(flag);
    }
}
