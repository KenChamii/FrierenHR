using FrierenHR.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrierenHR.Infrastructure.Data.Configurations;

public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.ToTable("Timesheets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Comment).HasMaxLength(1000);

        builder.HasOne(x => x.Employee).WithMany()
            .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DecidedByEmployee).WithMany()
            .HasForeignKey(x => x.DecidedByEmployeeId).OnDelete(DeleteBehavior.Restrict);

        // One timesheet per employee per week.
        builder.HasIndex(x => new { x.EmployeeId, x.WeekStartDate }).IsUnique();
    }
}
