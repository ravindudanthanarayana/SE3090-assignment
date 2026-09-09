using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartDesk.Api.Infrastructure;
using SmartDesk.Api.Middleware;
using SmartDesk.Application.Common;
using SmartDesk.Infrastructure;
using SmartDesk.Infrastructure.Persistence;
using SmartDesk.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Environment variables override appsettings, so secrets never need to live in a committed file.
builder.Configuration.AddEnvironmentVariables();

// ---- Services ---------------------------------------------------------------------------------

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Serialise enums as their names ("High", "AwaitingApproval") rather than integers. This keeps
        // the API self-describing for both clients, makes Swagger readable, and means a renumbered
        // enum can never silently change the meaning of a stored or transmitted value.
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IHttpContextAccessorAdapter, HttpContextAccessorAdapter>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<WorkflowRunner>();

builder.Services.AddSmartDesk(builder.Configuration);
builder.Services.AddScoped<DbSeeder>();

// ---- Authentication and authorization ----------------------------------------------------------

var jwt = new JwtOptions();
builder.Configuration.GetSection(JwtOptions.SectionName).Bind(jwt);
var jwtSecret = builder.Configuration["JWT_SECRET"];
if (!string.IsNullOrWhiteSpace(jwtSecret)) jwt.Secret = jwtSecret;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Every one of these is on deliberately: a token must be for this issuer, this audience,
            // unexpired, and signed with our key.
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ---- CORS -------------------------------------------------------------------------------------

const string CorsPolicy = "SmartDeskCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5173"];

builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
    // Explicit origins only. A future Flutter client is native and is not subject to CORS at all.
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ---- Swagger / OpenAPI -------------------------------------------------------------------------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartDesk AI API",
        Version = "v1",
        Description =
            "IT help desk and support management API. Shared by the React web application and the " +
            "Flutter mobile application. Hosts the Agentic AI workflow, its human approval gate and " +
            "the audit trail."
    });

    // Adds the Authorize button so every protected endpoint is testable directly from Swagger.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token returned by POST /api/auth/login."
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
    });

    var xml = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xml)) options.IncludeXmlComments(xml);
});

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

// ---- Pipeline ---------------------------------------------------------------------------------

// First, so it catches everything downstream.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger is enabled in all environments: the spec requires a working deployed Swagger URL.
app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartDesk AI API v1");
    o.DocumentTitle = "SmartDesk AI API";
});

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// ---- Startup migration and seeding --------------------------------------------------------------

// Skipped under the test host, which manages its own database lifecycle.
if (!app.Environment.IsEnvironment("Testing") &&
    app.Configuration.GetValue("RunMigrationsOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.MigrateAndSeedAsync();
}

app.Run();

/// <summary>Exposed so the integration tests can use WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;
