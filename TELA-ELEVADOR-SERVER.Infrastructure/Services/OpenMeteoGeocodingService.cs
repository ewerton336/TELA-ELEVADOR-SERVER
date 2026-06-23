using System.Text.Json;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Services;

public sealed class OpenMeteoGeocodingService : IGeocodingService
{
    private const string BaseUrl = "https://geocoding-api.open-meteo.com/v1/search";
    private const string EstadoPreferido = "São Paulo";

    private readonly HttpClient _httpClient;

    public OpenMeteoGeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GeocodingResult?> GeocodeAsync(string nomeCidade, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomeCidade))
            return null;

        var nome = nomeCidade.Split(',')[0].Trim();
        if (nome.Length == 0)
            return null;

        var url = $"{BaseUrl}?name={Uri.EscapeDataString(nome)}&count=10&language=pt&format=json";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var results)
            || results.ValueKind != JsonValueKind.Array
            || results.GetArrayLength() == 0)
        {
            return null;
        }

        var escolhido = results[0];
        foreach (var r in results.EnumerateArray())
        {
            if (r.TryGetProperty("admin1", out var admin1)
                && admin1.ValueKind == JsonValueKind.String
                && string.Equals(admin1.GetString(), EstadoPreferido, StringComparison.OrdinalIgnoreCase))
            {
                escolhido = r;
                break;
            }
        }

        if (escolhido.TryGetProperty("latitude", out var lat)
            && escolhido.TryGetProperty("longitude", out var lon)
            && lat.ValueKind == JsonValueKind.Number
            && lon.ValueKind == JsonValueKind.Number)
        {
            return new GeocodingResult(lat.GetDouble(), lon.GetDouble());
        }

        return null;
    }
}
