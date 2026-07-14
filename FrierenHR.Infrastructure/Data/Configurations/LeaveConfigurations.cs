using FrierenHR.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrierenHR.Infrastructure.Data.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Days).HasColumnType("decimal(6,2)");
        builder.HasOne(x => x.Employee).WithMany(e => e.LeaveRequests)
            .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        // Same self-reference shape as Employee.Manager — Restrict, or SQL Server
        // rejects the table (this FK and EmployeeId's FK both point at Employees).
        builder.HasOne(x => x.DecidedByEmployee).WithMany()
            .HasForeignKey(x => x.DecidedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EmployeeId, x.Status }); // powers the approver-queue lookup in 3.6
    }
}

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("LeaveBalances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Balance).HasColumnType("decimal(6,2)");
        builder.HasOne(x => x.Employee).WithMany(e => e.LeaveBalances)
            .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.EmployeeId, x.LeaveType }).IsUnique(); // one balance row per employee+type, matches UpsertBalanceAsync's lookup key
    }
}