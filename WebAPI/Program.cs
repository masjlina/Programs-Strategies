using ApplicationCore.Services;
using ApplicationCore.Services.IServices;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter());
    });
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ConnStr")
    )
);

builder.Services.AddScoped<IAdministrativeUnitService, AdministrativeUnitService>();
builder.Services.AddScoped<IStrategyService, StrategyService>();
builder.Services.AddScoped<IStrategicGoalService, StrategicGoalService>();
builder.Services.AddScoped<IOperationalGoalService, OperationalGoalService>();
builder.Services.AddScoped<IProgramTaskService, ProgramTaskService>();
builder.Services.AddScoped<IParseService, ParseService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var webApiXml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var applicationCoreXml = "ApplicationCore.xml";
    var sharedXml = "Shared.xml";
    var basePath = AppContext.BaseDirectory;

    options.IncludeXmlComments(Path.Combine(basePath, webApiXml));
    options.IncludeXmlComments(Path.Combine(basePath, applicationCoreXml));
    options.IncludeXmlComments(Path.Combine(basePath, sharedXml));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();