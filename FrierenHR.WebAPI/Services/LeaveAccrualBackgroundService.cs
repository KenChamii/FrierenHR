using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Application.Features.Leave;
using FrierenHR.Core.Enums;

namespace FrierenHR.WebAPI.Services;

public class LeaveAccrualBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LeaveAccrualBackgroundService> _logger;

    public LeaveAccrualBackgroundService(IServiceProvider serviceProvider, ILogger<LeaveAccrualBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAccrualForAllEmployeesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    public async Task RunAccrualForAllEmployeesAsync(CancellationToken ct = default)
    {
        // Scoped services (ILeaveService, repositories) can't be constructor-injected
        using var scope = _serviceProvider.CreateScope();
        var companyRepository = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
        var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveService>();

        var companies = await companyRepository.GetAllAsync(ct);
        foreach (var company in companies)
        {
            var employees = await employeeRepository.GetByCompanyAsync(company.Id, ct);
            foreach (var employee in employees.Where(e => e.IsActive))
            {
                foreach (var leaveType in Enum.GetValues<LeaveType>())
                {
                    try
                    {
                        await leaveService.RunAccrualForEmployeeAsync(employee.Id, leaveType, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Accrual failed for employee {EmployeeId}, leave type {LeaveType}", employee.Id, leaveType);
                    }
                }
            }
        }
    }
}