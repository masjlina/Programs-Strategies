using ApplicationCore.Services;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ConnStr")
    )
);

builder.Services.AddScoped<IAdministrativeUnitService, AdministrativeUnitService>();
builder.Services.AddScoped<IStrategyService, StrategyService>();
builder.Services.AddScoped<IStrategicGoalService, StrategicGoalService>();
builder.Services.AddScoped<IOperationalGoalService, OperationalGoalService>();
builder.Services.AddScoped<IProgramTaskService, ProgramTaskService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
