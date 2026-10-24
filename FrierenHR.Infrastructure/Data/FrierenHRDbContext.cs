using FrierenHR.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FrierenHR.Infrastructure.Data;

public class FrierenHRDbContext : DbContext
{
    public FrierenHRDbContext(DbContextOptions<FrierenHRDbContext> options) : base(options) { }

    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<CompanyRuleConfig> CompanyRuleConfigs => Set<CompanyRuleConfig>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();
    public DbSet<ApprovalChain> ApprovalChains => Set<ApprovalChain>();
    public DbSet<ApprovalInstance> ApprovalInstances => Set<ApprovalInstance>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<AttendanceCorrection> AttendanceCorrections => Set<AttendanceCorrection>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<PayrollDeduction> PayrollDeductions => Set<PayrollDeduction>();
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<PayslipLine> PayslipLines => Set<PayslipLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FrierenHRDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>Normalises every <see cref="DateTime"/> to UTC on the way into the database.</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        builder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
        base.ConfigureConventions(builder);
    }

}

/// <summary>Forces a <see cref="DateTime"/> to UTC for storage, and marks it UTC on read.</summary>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        value => ToUtc(value),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    { }

    internal static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,

        // A local time genuinely needs converting.
        DateTimeKind.Local => value.ToUniversalTime(),

        // Unspecified is what request JSON produces.
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}

/// <summary>The nullable counterpart of <see cref="UtcDateTimeConverter"/>.</summary>
internal sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter() : base(
        value => value.HasValue ? UtcDateTimeConverter.ToUtc(value.Value) : null,
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null)
    { }
}
