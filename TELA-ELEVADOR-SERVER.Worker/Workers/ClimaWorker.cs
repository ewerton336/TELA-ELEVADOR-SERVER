using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Worker.Workers;

public sealed class ClimaWorker : BackgroundService
{
    private readonly ILogger<ClimaWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _intervalMinutes;

    private static readonly Dictionary<int, (string Description, string Icon)> WeatherCodeMap = new()
    {
        { 0, ("Céu limpo", "☀️") },
        { 1, ("Principalmente limpo", "🌤️") },
        { 2, ("Parcialmente nublado", "⛅") },
        { 3, ("Nublado", "☁️") },
        { 45, ("Nevoeiro", "🌫️") },
        { 48, ("Nevoeiro depositador", "🌫️") },
        { 51, ("Garoa leve", "🌦️") },
        { 53, ("Garoa moderada", "🌦️") },
        { 55, ("Garoa densa", "🌧️") },
        { 61, ("Chuva leve", "🌧️") },
        { 63, ("Chuva moderada", "⛈️") },
        { 65, ("Chuva forte", "⛈️") },
        { 71, ("Neve leve", "❄️") },
        { 73, ("Neve moderada", "❄️") },
        { 75, ("Neve forte", "❄️") },
        { 80, ("Pancadas de chuva leve", "🌧️") },
        { 81, ("Pancadas de chuva moderada", "⛈️") },
        { 82, ("Pancadas de chuva violenta", "⛈️") },
        { 95, ("Tempestade", "⛈️") },
    };

    public ClimaWorker(ILogger<ClimaWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _intervalMinutes = 240; // 4 horas padrão
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ClimaWorker iniciado. Intervalo: {IntervalMinutes} minutos", _intervalMinutes);

        // Executar uma vez na inicialização
        await FetchAndStoreClimateAsync(stoppingToken);

        // Executar periodicamente
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_intervalMinutes));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FetchAndStoreClimateAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ClimaWorker cancelado");
        }
    }

    private async Task FetchAndStoreClimateAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Obter IDs de cidades distintas dos prédios cadastrados
            var cidadeIds = await dbContext.Predios
                .Where(p => p.CidadeId.HasValue)
                .Select(p => p.CidadeId!.Value)
                .Distinct()
                .ToListAsync(stoppingToken);

            // Carregar as cidades pelos IDs
            var cidades = await dbContext.Cidades
                .Where(c => cidadeIds.Contains(c.Id))
                .ToListAsync(stoppingToken);

            if (cidades.Count == 0)
            {
                _logger.LogInformation("Nenhuma cidade com prédios encontrada. Abortar busca de clima.");
                return;
            }

            _logger.LogInformation("Processando clima para {CidadeCount} cidade(s) com prédio(s) cadastrado(s)", cidades.Count);
            foreach (var cidade in cidades)
            {
                try
                {
                    await FetchAndStoreCityWeatherAsync(dbContext, cidade, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao buscar clima para cidade {CidadeNome} - {ErrorMessage}", cidade.NomeExibicao, ex.Message);
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Clima atualizado para {CidadeCount} cidade(s)", cidades.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar clima");
        }
    }

    private async Task FetchAndStoreCityWeatherAsync(AppDbContext dbContext, Cidade cidade, CancellationToken stoppingToken)
    {
        // Usar Open-Meteo API (gratuita, sem autenticação necessária)
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={cidade.Latitude}&longitude={cidade.Longitude}&daily=temperature_2m_max,temperature_2m_min,weather_code&temperature_unit=celsius&timezone=auto";

        _logger.LogInformation("Buscando clima para {CidadeNome} (Lat: {Latitude}, Lon: {Longitude})", cidade.NomeExibicao, cidade.Latitude, cidade.Longitude);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        var response = await client.GetAsync(url, stoppingToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(stoppingToken);

        // Parse simplificado do JSON (você pode usar System.Text.Json ou Newtonsoft.Json se preferir)
        var weatherData = ParseOpenMeteoResponse(json);

        if (weatherData != null)
        {
            // Remover previsões antigas
            var predicadasAntigas = dbContext.ClimaPrevisoesData
                .Where(cp => cp.CidadeId == cidade.Id)
                .ToList();

            foreach (var predicao in predicadasAntigas)
            {
                dbContext.ClimaPrevisoesData.Remove(predicao);
            }

            // Adicionar novas previsões
            foreach (var dia in weatherData.Dias)
            {
                var previsao = new ClimaPrevisao
                {
                    CidadeId = cidade.Id,
                    Data = dia.Data,
                    TemperaturaMaxima = (int)Math.Round(dia.TemperaturaMaxima),
                    TemperaturaMinima = (int)Math.Round(dia.TemperaturaMinima),
                    CodigoWmo = dia.CodigoWmo,
                    Descricao = dia.Descricao,
                    Icone = dia.Icone,
                    CriadoEm = DateTime.UtcNow,
                    AtualizadoEm = DateTime.UtcNow
                };

                dbContext.ClimaPrevisoesData.Add(previsao);
            }

            _logger.LogInformation("Clima carregado para {CidadeNome}: {DiaCount} dias", cidade.NomeExibicao, weatherData.Dias.Count);
        }
    }

    private class WeatherData
    {
        public List<DayWeather> Dias { get; set; } = new();
    }

    private class DayWeather
    {
        public DateOnly Data { get; set; }
        public double TemperaturaMaxima { get; set; }
        public double TemperaturaMinima { get; set; }
        public int CodigoWmo { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string Icone { get; set; } = string.Empty;
    }

    private WeatherData? ParseOpenMeteoResponse(string json)
    {
        try
        {
            // Parse simplificado usando System.Text.Json
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("daily", out var daily))
                return null;

            var data = new WeatherData();

            if (daily.TryGetProperty("time", out var times) &&
                daily.TryGetProperty("temperature_2m_max", out var tempMaxes) &&
                daily.TryGetProperty("temperature_2m_min", out var tempMins) &&
                daily.TryGetProperty("weather_code", out var weatherCodes))
            {
                var timeArray = times.EnumerateArray().ToList();
                var tempMaxArray = tempMaxes.EnumerateArray().ToList();
                var tempMinArray = tempMins.EnumerateArray().ToList();
                var codeArray = weatherCodes.EnumerateArray().ToList();

                for (int i = 0; i < Math.Min(timeArray.Count, 7); i++)
                {
                    if (DateOnly.TryParse(timeArray[i].GetString(), out var dateValue))
                    {
                        var code = (int)codeArray[i].GetDouble();
                        var (description, icon) = WeatherCodeMap.TryGetValue(code, out var info)
                            ? info
                            : ("Desconhecido", "❓");

                        data.Dias.Add(new DayWeather
                        {
                            Data = dateValue,
                            TemperaturaMaxima = tempMaxArray[i].GetDouble(),
                            TemperaturaMinima = tempMinArray[i].GetDouble(),
                            CodigoWmo = code,
                            Descricao = description,
                            Icone = icon
                        });
                    }
                }
            }

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer parse da resposta Open-Meteo");
            return null;
        }
    }
}
