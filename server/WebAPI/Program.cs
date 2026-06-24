using Application.Services;
using Application.Services.IServices;
using Application.Dtos;
using Application.Dtos.Mappers;
using Domain.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

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

builder.Services.AddScoped<ICrudService<RegionDto>, RegionService>();
builder.Services.AddScoped<ICrudService<DistrictDto>, CrudService<District, DistrictDto>>();
builder.Services.AddScoped<ICrudService<CommunityDto>, CommunityService>();
builder.Services.AddScoped<ICrudService<SettlementDto>, CrudService<Settlement, SettlementDto>>();
builder.Services.AddScoped<IStrategyService, StrategyService>();
builder.Services.AddScoped<IStrategicGoalService, StrategicGoalService>();
builder.Services.AddScoped<IOperationalGoalService, OperationalGoalService>();
builder.Services.AddScoped<IProgramTaskService, ProgramTaskService>();
builder.Services.AddScoped<IParseService, ParseService>();
builder.Services.AddScoped<IOfficialDataImportService, OfficialDataImportService>();
builder.Services.AddScoped<ISystemStatsService, SystemStatsService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpClient<ILemmatizer, UkrainianLemmatizer>(client =>
{
    var url = builder.Configuration["Services:NlpServiceUrl"] ?? "http://localhost:8082";
    client.BaseAddress = new Uri(url);
});
builder.Services.AddScoped<TextProcessingPipeline>();
builder.Services.AddScoped<IStrategyImportService, StrategyImportService>();
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
    var applicationXml = "Application.xml";
    var sharedXml = "Shared.xml";
    var basePath = AppContext.BaseDirectory;

    options.IncludeXmlComments(Path.Combine(basePath, webApiXml));
    options.IncludeXmlComments(Path.Combine(basePath, applicationXml));
    options.IncludeXmlComments(Path.Combine(basePath, sharedXml));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
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



var apiSettingsSection = builder.Configuration.GetSection("Authentication:Schemes:Bearer");
builder.Services.Configure<ApiSettings>(option =>
{
    option.ValidIssuer = apiSettingsSection["ValidIssuer"];
    option.ValidAudiences = apiSettingsSection.GetSection("ValidAudiences").Get<string[]>();
    option.SigningKey = apiSettingsSection["SigningKeys:0:Value"];
});

var apiSettings = apiSettingsSection.Get<ApiSettings>();
var signingKey = Convert.FromBase64String(apiSettingsSection["SigningKeys:0:Value"]);

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = apiSettings.ValidIssuer,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(signingKey),
        ValidateAudience = true,
        ValidAudiences = apiSettings.ValidAudiences,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ProgramsStrategies");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var usersJsonPath = Path.Combine(app.Environment.ContentRootPath, "users.json");
    if (File.Exists(usersJsonPath))
    {
        try
        {
            var jsonString = File.ReadAllText(usersJsonPath);
            var seedUsers = JsonSerializer.Deserialize<List<SeedUserDto>>(jsonString);
            if (seedUsers != null)
            {
                foreach (var seedUser in seedUsers)
                {
                    if (!db.Users.Any(u => u.Email == seedUser.Email))
                    {
                        db.Users.Add(new User
                        {
                            Email = seedUser.Email,
                            PasswordHash = PasswordHasher.HashPassword(seedUser.Password),
                            Role = seedUser.Role
                        });
                    }
                }
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding users: {ex.Message}");
        }
    }
}

app.Run();

public class SeedUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
