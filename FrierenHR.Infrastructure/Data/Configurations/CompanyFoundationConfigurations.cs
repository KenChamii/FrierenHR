using FrierenHR.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrierenHR.Infrastructure.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.HasOne(x => x.Company).WithMany(c => c.Departments)
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.LastName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(x => x.Email).IsUnique();

        builder.HasOne(x => x.Company).WithMany(c => c.Employees)
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Department).WithMany(d => d.Employees)
            .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);

        // The self-referencing FK — Restrict is not optional here, SQL Server
        // refuses to create the table with Cascade/SetNull on a self-join.
        builder.HasOne(x => x.Manager).WithMany()
            .HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
    }
}