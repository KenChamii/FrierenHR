using FrierenHR.Application.Features.RulesConfig;
using FrierenHR.Core.RulesEngine;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FrierenHRDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddScoped<IRuleConfigRepository, RuleConfigRepository>();
builder.Services.AddScoped<IRuleConfigService, RuleConfigService>();
builder.Services.AddSingleton<IRuleEvaluator, RuleEvaluator>();


var app = builder.Build();
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FrierenHRDbContext>();
    await DbSeeder.SeedAsync(db);
}
app.UseHttpsRedirection();
app.MapControllers();
app.Run();