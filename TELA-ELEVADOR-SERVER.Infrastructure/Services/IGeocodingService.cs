namespace TELA_ELEVADOR_SERVER.Infrastructure.Services;

public sealed record GeocodingResult(double Latitude, double Longitude);

public interface IGeocodingService
{
    Task<GeocodingResult?> GeocodeAsync(string nomeCidade, CancellationToken cancellationToken = default);
}
