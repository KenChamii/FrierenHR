using System.Text;
using System.Threading.RateLimiting;
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
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Fail at startup with a named error rather than later with an obscure one. An unset Jwt:Key
// would otherwise produce an empty signing key and tokens nobody can validate.
var connectionString = Required(builder.Configuration.GetConnectionString("DefaultConnection"), "ConnectionStrings:DefaultConnection");
var jwtKey = Required(builder.Configuration["Jwt:Key"], "Jwt:Key");
if (jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<long random string>\"");

builder.Services.AddDbContext<FrierenHRDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<TokenService>();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true
        };

        // SignalR cannot send an Authorization header on its fallback transports, so the client
        // passes the token as ?access_token=. Without this, every hub connection is anonymous
        // and ChatHub's [Authorize] rejects it.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

// Login is anonymous, so throttle it per IP to make password guessing expensive.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// Anything uncaught returns a consistent ProblemDetails and never leaks a stack trace
// outside Development.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Reports unhealthy when the database is unreachable, which is the failure a load balancer
// most needs to see. Written inline to avoid a package reference just for AddDbContextCheck.
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

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

// Allowed origins come from config so pointing at a different frontend is not a code change.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(o => o.AddPolicy("AngularApp", p => p
    .WithOrigins(corsOrigins)
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseExceptionHandler(); // early, so it wraps everything downstream

// Migrations run in every environment: a deploy against an empty database used to start and
// then fail on first query. Seeding stays in Development only. For a controlled rollout,
// generate a migration bundle and run it as a deploy step instead of relying on this.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FrierenHRDbContext>();
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment()) await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // serves wwwroot/uploads for chat attachments
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AngularApp");
app.UseRateLimiter();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();
app.MapHealthChecks("/health");
app.Run();

static string Required(string? value, string key) =>
    string.IsNullOrWhiteSpace(value)
        ? throw new InvalidOperationException(
            $"Configuration '{key}' is missing. Set it with: dotnet user-secrets set \"{key}\" \"<value>\", " +
            $"or as the environment variable {key.Replace(":", "__")}.")
        : value;

/// <summary>Health check that confirms the database is reachable.</summary>
internal sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly FrierenHRDbContext _context;
    public DatabaseHealthCheck(FrierenHRDbContext context) => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(ct)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check threw.", ex);
        }
    }
}
