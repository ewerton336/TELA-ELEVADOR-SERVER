namespace TELA_ELEVADOR_SERVER.Domain.Weather;

public static class WeatherCodeTranslator
{
    public const string UnknownDescription = "Desconhecido";
    public const string UnknownIcon = "❓";

    private static readonly IReadOnlyDictionary<int, (string Description, string Icon)> Map =
        new Dictionary<int, (string Description, string Icon)>
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
            { 56, ("Garoa congelante leve", "🌨️") },
            { 57, ("Garoa congelante densa", "🌨️") },
            { 61, ("Chuva leve", "🌧️") },
            { 63, ("Chuva moderada", "⛈️") },
            { 65, ("Chuva forte", "⛈️") },
            { 66, ("Chuva congelante leve", "🌨️") },
            { 67, ("Chuva congelante forte", "🌨️") },
            { 71, ("Neve leve", "❄️") },
            { 73, ("Neve moderada", "❄️") },
            { 75, ("Neve forte", "❄️") },
            { 77, ("Grãos de neve", "❄️") },
            { 80, ("Pancadas de chuva leve", "🌧️") },
            { 81, ("Pancadas de chuva moderada", "⛈️") },
            { 82, ("Pancadas de chuva violenta", "⛈️") },
            { 85, ("Pancadas de neve leve", "🌨️") },
            { 86, ("Pancadas de neve fortes", "❄️") },
            { 95, ("Tempestade", "⛈️") },
            { 96, ("Tempestade com granizo leve", "⛈️") },
            { 99, ("Tempestade com granizo forte", "⛈️") },
        };

    public static (string Description, string Icon) Translate(int code)
        => Map.TryGetValue(code, out var info) ? info : (UnknownDescription, UnknownIcon);
}
