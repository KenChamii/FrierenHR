using FrierenHR.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrierenHR.Infrastructure.Data.Configurations;

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade: deleting a department should not silently remove the
        // holidays its people were counting on.
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // One record per date per scope. A company-wide entry and a department entry on the
        // same date are both allowed; two identical ones are not.
        builder.HasIndex(x => new { x.CompanyId, x.Date, x.DepartmentId }).IsUnique();

        // Every lookup is "this company, this range", so lead with those two.
        builder.HasIndex(x => new { x.CompanyId, x.Date });
    }
}

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict both ways: a shift in use cannot be deleted out from under the people
        // assigned to it. The service tells the caller to reassign first.
        builder.HasMany(x => x.Employees).WithOne(e => e.Shift).HasForeignKey(e => e.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Departments).WithOne(d => d.Shift).HasForeignKey(d => d.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();

        // A filtered unique index: at most one default per company, with the rest unaffected.
        // Raw SQL, so PostgreSQL syntax. See the note in PayrollConfigurations.
        builder.HasIndex(x => x.CompanyId)
            .IsUnique()
            .HasFilter("\"IsDefault\" = true")
            .HasDatabaseName("IX_Shifts_CompanyId_SingleDefault");
    }
}

public class AttendanceCorrectionConfiguration : IEntityTypeConfiguration<AttendanceCorrection>
{
    public void Configure(EntityTypeBuilder<AttendanceCorrection> builder)
    {
        builder.ToTable("AttendanceCorrections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DecisionComment).HasMaxLength(500);

        // Restrict rather than Cascade, for two reasons. SQL Server refuses two cascade paths
        // from Employees to this table, and there is a second one through AttendanceLogs.
        // It is also the behaviour we want: a correction records who asked for a change to
        // their hours and why, so it should outlive a deleted account rather than vanish
        // with it. Employees are deactivated through IsActive, not deleted, so this does not
        // get in anyone's way in practice.
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict: an approver's account is part of the record of who decided what.
        builder.HasOne(x => x.DecidedByEmployee).WithMany().HasForeignKey(x => x.DecidedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AttendanceLog).WithMany().HasForeignKey(x => x.AttendanceLogId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.EmployeeId, x.Date });
        builder.HasIndex(x => x.Status);
    }
}

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Changes).IsRequired();

        // No navigation to Company or Employee on purpose. The trail outlives the records it
        // describes, and a foreign key would either block their deletion or erase the history.
        builder.HasIndex(x => new { x.CompanyId, x.ChangedAt });
        builder.HasIndex(x => new { x.EntityName, x.EntityId });
    }
}
