using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TELA_ELEVADOR_SERVER.Infrastructure.Persistence;

namespace TELA_ELEVADOR_SERVER.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var migrationsAssembly = typeof(AppDbContext).Assembly.GetName().Name;
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(migrationsAssembly)));
        }

        return services;
    }
}
