using Microsoft.EntityFrameworkCore;
using Serilog;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;
using TELA_ELEVADOR_SERVER.Infrastructure;
using TELA_ELEVADOR_SERVER.Infrastructure.Services;
using TELA_ELEVADOR_SERVER.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.AddSerilog();

// Configuração de Banco de Dados
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    var migrationsAssembly = typeof(AppDbContext).Assembly.GetName().Name;
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(migrationsAssembly)));
}

// Registrar serviços
builder.Services.AddScoped<CidadeService>();
builder.Services.AddHttpClient();

// Registrar Workers
builder.Services.AddHostedService<ClimaWorker>();
builder.Services.AddHostedService<NoticiasWorker>();

var host = builder.Build();

try
{
    Log.Information("Iniciando TELA-ELEVADOR-SERVER Worker");
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação terminou com erro");
}
finally
{
    Log.CloseAndFlush();
}
