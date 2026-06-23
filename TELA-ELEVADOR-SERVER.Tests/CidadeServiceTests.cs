using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;
using TELA_ELEVADOR_SERVER.Infrastructure.Services;

namespace TELA_ELEVADOR_SERVER.Tests;

public class CidadeServiceTests
{
    private static AppDbContext NovoDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"cidade_test_{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static Mock<IGeocodingService> GeocodingQueRetorna(double lat, double lon)
    {
        var mock = new Mock<IGeocodingService>();
        mock.Setup(g => g.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeocodingResult(lat, lon));
        return mock;
    }

    [Fact]
    public async Task GetOrCreate_CidadeExistenteComCoordenadasZero_DeveCorrigirViaGeocoding()
    {
        await using var db = NovoDbContext();
        db.Cidades.Add(new Cidade
        {
            Nome = "praia grande",
            NomeExibicao = "Praia Grande, SP",
            Latitude = 0,
            Longitude = 0,
            CriadoEm = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var geocoding = GeocodingQueRetorna(-24.0058, -46.4028);
        var service = new CidadeService(db, geocoding.Object);

        var cidade = await service.GetOrCreateCidadeNormalizedAsync("Praia Grande");

        cidade.Latitude.Should().Be(-24.0058);
        cidade.Longitude.Should().Be(-46.4028);

        var persistida = await db.Cidades.SingleAsync(c => c.Nome == "praia grande");
        persistida.Latitude.Should().Be(-24.0058);
        persistida.Longitude.Should().Be(-46.4028);
    }

    [Fact]
    public async Task GetOrCreate_CidadeExistenteComCoordenadasValidas_NaoDeveChamarGeocoding()
    {
        await using var db = NovoDbContext();
        db.Cidades.Add(new Cidade
        {
            Nome = "praia grande",
            NomeExibicao = "Praia Grande, SP",
            Latitude = -24.0058,
            Longitude = -46.4028,
            CriadoEm = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var geocoding = GeocodingQueRetorna(0, 0);
        var service = new CidadeService(db, geocoding.Object);

        var cidade = await service.GetOrCreateCidadeNormalizedAsync("Praia Grande");

        cidade.Latitude.Should().Be(-24.0058);
        cidade.Longitude.Should().Be(-46.4028);
        geocoding.Verify(g => g.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreate_CidadeNova_DeveCriarComCoordenadasDoGeocoding()
    {
        await using var db = NovoDbContext();
        var geocoding = GeocodingQueRetorna(-23.9608, -46.3336);
        var service = new CidadeService(db, geocoding.Object);

        var cidade = await service.GetOrCreateCidadeNormalizedAsync("Santos");

        cidade.Latitude.Should().Be(-23.9608);
        cidade.Longitude.Should().Be(-46.3336);

        var persistida = await db.Cidades.SingleAsync(c => c.Nome == "santos");
        persistida.Latitude.Should().Be(-23.9608);
        persistida.Longitude.Should().Be(-46.3336);
    }
}
