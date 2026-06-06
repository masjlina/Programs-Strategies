using ApplicationCore.Services;
using ApplicationCore.Services.IServices;
using ApplicationCore.Dtos;
using ApplicationCore.Dtos.Mappers;
using Infrastructure.Data;
using Infrastructure.Entities;
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

builder.Services.AddScoped<ICrudService<RegionDto>, CrudService<Region, RegionDto>>();
builder.Services.AddScoped<ICrudService<DistrictDto>, CrudService<District, DistrictDto>>();
builder.Services.AddScoped<ICrudService<CommunityDto>, CrudService<Community, CommunityDto>>();
builder.Services.AddScoped<ICrudService<SettlementDto>, CrudService<Settlement, SettlementDto>>();
builder.Services.AddScoped<IStrategyService, StrategyService>();
builder.Services.AddScoped<IStrategicGoalService, StrategicGoalService>();
builder.Services.AddScoped<IOperationalGoalService, OperationalGoalService>();
builder.Services.AddScoped<IProgramTaskService, ProgramTaskService>();
builder.Services.AddScoped<IParseService, ParseService>();
builder.Services.AddScoped<IOfficialDataImportService, OfficialDataImportService>();
builder.Services.AddScoped<IMapper<Region, RegionDto>, RegionMapper>();
builder.Services.AddScoped<IMapper<District, DistrictDto>, DistrictMapper>();
builder.Services.AddScoped<IMapper<Community, CommunityDto>, CommunityMapper>();
builder.Services.AddScoped<IMapper<Settlement, SettlementDto>, SettlementMapper>();
builder.Services.AddScoped<IMapper<Strategy, StrategyDto>, StrategyMapper>();
builder.Services.AddScoped<IMapper<StrategicGoal, StrategicGoalDto>, StrategicGoalMapper>();
builder.Services.AddScoped<IMapper<OperationalGoal, OperationalGoalDto>, OperationalGoalMapper>();
builder.Services.AddScoped<IMapper<ProgramTask, ProgramTaskDto>, ProgramTaskMapper>();

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

var allowedOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (builder.Environment.IsDevelopment())
{
    allowedOrigins = allowedOrigins
        .Concat(["http://localhost:5173"])
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Missing CORS configuration. Set Cors:AllowedOrigins.");
}

builder.Services.AddCors(o => o.AddPolicy("ProgramsStrategies", policy =>
{
    policy.WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
}));
var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ProgramsStrategies");
app.UseHttpsRedirection();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
