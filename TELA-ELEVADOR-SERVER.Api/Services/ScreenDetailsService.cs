using System.Collections.Concurrent;

namespace TELA_ELEVADOR_SERVER.Api.Services;

/// <summary>
/// Guarda em memória o último conjunto de detalhes (resolução, zoom, viewport,
/// orientação etc.) reportado por cada tela do elevador, com a data/hora da
/// captura — para o admin consultar a "última informação obtida".
/// </summary>
public sealed class ScreenDetailsService
{
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, ScreenDetailsEntry> _details = new();

    /// <param name="json">JSON cru com os detalhes coletados na tela do elevador.</param>
    public void Save(string deviceId, string json)
    {
        _details[deviceId] = new ScreenDetailsEntry(json, DateTime.UtcNow);
    }

    public ScreenDetailsEntry? Get(string deviceId)
    {
        Purge();
        if (!string.IsNullOrWhiteSpace(deviceId)
            && _details.TryGetValue(deviceId, out var entry)
            && DateTime.UtcNow - entry.CapturedAt <= Retention)
        {
            return entry;
        }
        return null;
    }

    /// <summary>
    /// Data/hora dos últimos detalhes de cada tela (para exibir "último obtido").
    /// </summary>
    public IReadOnlyDictionary<string, DateTime> GetTimestamps()
    {
        Purge();
        return _details.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.CapturedAt);
    }

    private void Purge()
    {
        var cutoff = DateTime.UtcNow - Retention;
        foreach (var kvp in _details)
        {
            if (kvp.Value.CapturedAt < cutoff)
            {
                _details.TryRemove(kvp.Key, out _);
            }
        }
    }
}

public sealed record ScreenDetailsEntry(string Json, DateTime CapturedAt);
