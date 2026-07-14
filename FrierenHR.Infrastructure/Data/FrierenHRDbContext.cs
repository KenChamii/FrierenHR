using FrierenHR.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FrierenHRDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

}