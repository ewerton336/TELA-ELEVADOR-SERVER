using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TELA_ELEVADOR_SERVER.Application.Noticias;
using TELA_ELEVADOR_SERVER.Api.Authorization;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;
using TELA_ELEVADOR_SERVER.Infrastructure.Noticias;
using TELA_ELEVADOR_SERVER.Infrastructure.Seeding;
using TELA_ELEVADOR_SERVER.Infrastructure.Security;
using TELA_ELEVADOR_SERVER.Infrastructure.Services;
using TELA_ELEVADOR_SERVER.Api.Hubs;
using TELA_ELEVADOR_SERVER.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var maxRequestBodySizeMb = Math.Max(1, builder.Configuration.GetValue<int>("KestrelLimits:MaxRequestBodySizeMB", 30));

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodySizeMb * 1024L * 1024L;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true));
});

builder.Services.AddHealthChecks();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddScoped<CidadeService>();
builder.Services.AddScoped<INoticiaProvider, G1NoticiaProvider>();
builder.Services.AddScoped<INoticiaProvider, SantaPortalNoticiaProvider>();
builder.Services.AddScoped<INoticiaProvider, DiarioLitoralNoticiaProvider>();
builder.Services.AddScoped<INoticiaService, NoticiaService>();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IDbSeeder, DbSeeder>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key") ?? string.Empty;
var jwtIssuer = jwtSection.GetValue<string>("Issuer");
var jwtAudience = jwtSection.GetValue<string>("Audience");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PredioMatchesSlug", policy =>
        policy.Requirements.Add(new PredioMatchesSlugRequirement()));
    options.AddPolicy("DeveloperOnly", policy =>
        policy.RequireClaim(ClaimTypes.Role, "Developer"));
});

builder.Services.AddSingleton<IAuthorizationHandler, PredioMatchesSlugHandler>();

builder.Services.AddSignalR();
builder.Services.AddSingleton<ScreenMonitorService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Ensure media directory exists
var mediaBasePath = builder.Configuration.GetValue<string>("MediaStorage:BasePath") ?? "media";
Directory.CreateDirectory(mediaBasePath);

var app = builder.Build();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TELA-ELEVADOR API v1");
    c.RoutePrefix = "swagger";
});
app.MapSwagger();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok("TELA-ELEVADOR API online"));
app.MapControllers();
app.MapHub<PredioHub>("/hub/predio");

using (var scope = app.Services.CreateScope())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        logger.LogInformation(">>> [STARTUP] Iniciando migração do banco de dados...");
        dbContext.Database.Migrate();
        logger.LogInformation(">>> [STARTUP] Migração concluída.");
    }
    catch (Exception ex)
    {
        // Keeps API alive even when database is temporarily unavailable.
        logger.LogError(ex, ">>> [STARTUP] Falha ao aplicar migração. API continuará em execução e tentará reconectar sob demanda.");
    }

    if (app.Environment.IsDevelopment())
    {
        logger.LogInformation(">>> [STARTUP] Ambiente: DEVELOPMENT - Iniciando DbSeeder (dados de teste)...");
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation(">>> [STARTUP] DbSeeder concluído.");
    }
}

app.Run();
