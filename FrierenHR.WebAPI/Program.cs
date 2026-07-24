using FrierenHR.Application.Features.Approval;
using FrierenHR.Application.Features.Attendance;
using FrierenHR.Application.Features.Company;
using FrierenHR.Application.Features.Employee;
using FrierenHR.Application.Features.Leave;
using FrierenHR.Application.Features.Messaging;
using FrierenHR.Application.Features.RulesConfig;
using FrierenHR.Application.Features.Timesheet;
using FrierenHR.Core.RulesEngine;
using FrierenHR.Infrastructure.Data;
using FrierenHR.Infrastructure.Repositories;
using FrierenHR.WebAPI.Common;
using FrierenHR.WebAPI.Hubs;
using FrierenHR.WebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FrierenHRDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<FrierenHR.WebAPI.Services.TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

// Global error handling: anything that isn't caught locally in a controller falls through
// to GlobalExceptionHandler, which returns a consistent ProblemDetails response and never
// leaks stack traces/connection details outside of Development.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddScoped<IRuleConfigRepository, RuleConfigRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IRuleConfigService, RuleConfigService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddSingleton<IRuleEvaluator, RuleEvaluator>();
builder.Services.AddSingleton<LeaveAccrualBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<LeaveAccrualBackgroundService>());
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IApprovalRepository, ApprovalRepository>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IMessagingRepository, MessagingRepository>();
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder.Services.AddScoped<ITimesheetRepository, TimesheetRepository>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();

// Allowed origins come from config (Cors:AllowedOrigins in appsettings.json / environment
// variables), not a hardcoded value, so this doesn't need a code change + redeploy to point
// at a different frontend URL per environment. Falls back to the Angular dev server if unset.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(o => o.AddPolicy("AngularApp", p => p
    .WithOrigins(corsOrigins)
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseExceptionHandler(); // must be early, so it wraps everything downstream
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FrierenHRDbContext>();
    await db.Database.MigrateAsync(); // apply any pending EF migrations before seeding/serving
    await DbSeeder.SeedAsync(db);
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AngularApp");
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.Run();