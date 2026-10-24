using FrierenHR.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrierenHR.Infrastructure.Data.Configurations;

public class PayrollDeductionConfiguration : IEntityTypeConfiguration<PayrollDeduction>
{
    public void Configure(EntityTypeBuilder<PayrollDeduction> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

        // Money and percentages both need exact arithmetic.
        builder.Property(x => x.Value).HasPrecision(18, 4);

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // One name per company, among the active ones.
        builder.HasIndex(x => new { x.CompanyId, x.Name })
            .IsUnique()
            .HasFilter("\"IsActive\" = true");
    }
}

public class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.Property(x => x.PeriodStart).HasColumnType("date");
        builder.Property(x => x.PeriodEnd).HasColumnType("date");

        builder.Property(x => x.HourlyRate).HasPrecision(18, 4);
        builder.Property(x => x.MonthlyRate).HasPrecision(18, 2);
        builder.Property(x => x.RegularHours).HasPrecision(18, 2);
        builder.Property(x => x.PayableOvertimeHours).HasPrecision(18, 2);
        builder.Property(x => x.GrossPay).HasPrecision(18, 2);
        builder.Property(x => x.TotalDeductions).HasPrecision(18, 2);
        builder.Property(x => x.NetPay).HasPrecision(18, 2);

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict, for the same two reasons as the attendance corrections table.
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // One payslip per employee per period.
        builder.HasIndex(x => new { x.EmployeeId, x.PeriodStart, x.PeriodEnd }).IsUnique();

        builder.HasMany(x => x.Lines).WithOne(x => x.Payslip).HasForeignKey(x => x.PayslipId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PayslipLineConfiguration : IEntityTypeConfiguration<PayslipLine>
{
    public void Configure(EntityTypeBuilder<PayslipLine> builder)
    {
        builder.Property(x => x.Label).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Basis).HasMaxLength(200);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
    }
}
