using System.Collections.Concurrent;

namespace TELA_ELEVADOR_SERVER.Api.Services;

public sealed class ScreenshotService
{
    // Mantemos o último print por tela por bastante tempo para que o admin possa
    // consultar a "última captura obtida" (com data/hora) muito depois de pedida.
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, ScreenshotEntry> _shots = new();

    public void Save(string deviceId, byte[] bytes, string contentType)
    {
        _shots[deviceId] = new ScreenshotEntry(bytes, contentType, DateTime.UtcNow);
    }

    public ScreenshotEntry? Get(string deviceId)
    {
        Purge();
        if (!string.IsNullOrWhiteSpace(deviceId)
            && _shots.TryGetValue(deviceId, out var entry)
            && DateTime.UtcNow - entry.CapturedAt <= Retention)
        {
            return entry;
        }
        return null;
    }

    /// <summary>
    /// Data/hora do último print de cada tela (para exibir "último obtido" no monitor).
    /// </summary>
    public IReadOnlyDictionary<string, DateTime> GetTimestamps()
    {
        Purge();
        return _shots.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.CapturedAt);
    }

    private void Purge()
    {
        var cutoff = DateTime.UtcNow - Retention;
        foreach (var kvp in _shots)
        {
            if (kvp.Value.CapturedAt < cutoff)
            {
                _shots.TryRemove(kvp.Key, out _);
            }
        }
    }
}

public sealed record ScreenshotEntry(byte[] Bytes, string ContentType, DateTime CapturedAt);
