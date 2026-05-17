using ApplicationCore.Services;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ConnStr")
    )
);

builder.Services.AddScoped<IAdministrativeUnitService, AdministrativeUnitService>();
builder.Services.AddScoped<IStrategyService, StrategyService>();
builder.Services.AddScoped<IStrategicGoalService, StrategicGoalService>();
builder.Services.AddScoped<IOperationalGoalService, OperationalGoalService>();
builder.Services.AddScoped<IProgramTaskService, ProgramTaskService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();
