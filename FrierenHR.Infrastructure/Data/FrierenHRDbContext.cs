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

    /// <summary>
    /// Normalises every <see cref="DateTime"/> to UTC on the way into the database.
    /// </summary>
    /// <remarks>
    /// Npgsql maps DateTime to "timestamp with time zone", which accepts only a value whose
    /// Kind is Utc. The application's own code is consistently UTC and satisfies that, but a
    /// date arriving in request JSON is deserialised with Kind=Unspecified and would be
    /// rejected at save time on a column the caller never chose.
    ///
    /// The converter settles that in one place rather than leaving it to every call site.
    /// An Unspecified value is labelled UTC without shifting it, which preserves the wall
    /// clock exactly as the previous SQL Server mapping did.
    /// </remarks>
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

        // A local time genuinely needs converting. The codebase does not produce one today,
        // so this is here to be correct rather than because it is reached.
        DateTimeKind.Local => value.ToUniversalTime(),

        // Unspecified is what request JSON produces. Labelled, not shifted: shifting would
        // move an attendance time by the server's offset, which is a silent data error.
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
