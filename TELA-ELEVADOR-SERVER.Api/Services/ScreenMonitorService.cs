using System.Collections.Concurrent;

namespace TELA_ELEVADOR_SERVER.Api.Services;

public sealed class ScreenMonitorService
{
    /// <summary>
    /// Por quanto tempo mantemos os dados da última sessão de uma tela após ela
    /// desconectar. Durante essa janela a tela continua visível no monitor (como
    /// "offline") e o admin ainda pode agendar um comando de atualização que será
    /// entregue assim que ela reconectar.
    /// </summary>
    public static readonly TimeSpan RetentionWindow = TimeSpan.FromHours(8);

    // Chaveado por deviceId — identidade estável da tela entre reconexões
    // (o connectionId muda a cada conexão).
    private readonly ConcurrentDictionary<string, ScreenInfo> _screens = new();

    /// <summary>
    /// Registra (ou reconecta) uma tela. A identidade estável é o deviceId; ao
    /// reconectar reaproveitamos o registro existente, preservando um eventual
    /// refresh pendente para entrega imediata.
    /// </summary>
    public RegisterResult Register(string connectionId, string slug, string? userAgent, string? appVersion = null, string? deviceId = null)
    {
        PurgeExpired();

        var normalizedDeviceId = string.IsNullOrWhiteSpace(deviceId) ? connectionId : deviceId;
        var now = DateTime.UtcNow;
        var evicted = new List<string>();
        var hadPendingRefresh = false;

        if (_screens.TryGetValue(normalizedDeviceId, out var existing))
        {
            // Mesma tela reconectando com novo connectionId — remove o antigo do grupo.
            if (!string.IsNullOrEmpty(existing.ConnectionId)
                && !string.Equals(existing.ConnectionId, connectionId, StringComparison.Ordinal))
            {
                evicted.Add(existing.ConnectionId);
            }

            hadPendingRefresh = existing.PendingRefresh;

            existing.ConnectionId = connectionId;
            existing.Slug = slug;
            existing.ConnectedAt = now;       // reinicia o uptime da sessão
            existing.LastHeartbeat = now;
            existing.Uptime = 0;
            existing.Connected = true;
            existing.DisconnectedAt = null;
            existing.PendingRefresh = false;  // refresh pendente é consumido ao reconectar
            if (userAgent is not null) existing.UserAgent = userAgent;
            if (appVersion is not null) existing.AppVersion = appVersion;

            return new RegisterResult(existing, evicted, hadPendingRefresh);
        }

        var info = new ScreenInfo
        {
            ConnectionId = connectionId,
            DeviceId = normalizedDeviceId,
            Slug = slug,
            ConnectedAt = now,
            LastHeartbeat = now,
            Uptime = 0,
            Connected = true,
            DisconnectedAt = null,
            PendingRefresh = false,
            UserAgent = userAgent,
            AppVersion = appVersion
        };
        _screens[normalizedDeviceId] = info;

        return new RegisterResult(info, evicted, hadPendingRefresh);
    }

    public void UpdateHeartbeat(string connectionId, double uptime, bool isVisible, string? appVersion = null)
    {
        var info = FindByConnectionId(connectionId);
        if (info is null) return;

        info.LastHeartbeat = DateTime.UtcNow;
        info.Uptime = uptime;
        info.IsVisible = isVisible;
        info.Connected = true;
        info.DisconnectedAt = null;
        if (appVersion is not null)
            info.AppVersion = appVersion;
    }

    /// <summary>
    /// Marca a tela como desconectada sem removê-la — os dados permanecem por
    /// <see cref="RetentionWindow"/>. Retorna a tela afetada, ou null se o
    /// connectionId já não corresponde à sessão atual (ex.: já reconectou com
    /// outro connectionId).
    /// </summary>
    public ScreenInfo? MarkDisconnected(string connectionId)
    {
        var info = FindByConnectionId(connectionId);
        if (info is null) return null;

        info.Connected = false;
        info.DisconnectedAt = DateTime.UtcNow;
        return info;
    }

    /// <summary>
    /// Solicita a atualização (reload) de uma tela pelo deviceId. Se estiver
    /// online, retorna o connectionId atual para envio imediato; se estiver
    /// offline, marca um refresh pendente que será entregue na reconexão.
    /// </summary>
    public RefreshRequestResult RequestRefresh(string deviceId)
    {
        PurgeExpired();

        if (string.IsNullOrWhiteSpace(deviceId) || !_screens.TryGetValue(deviceId, out var info))
        {
            return new RefreshRequestResult(Found: false, Connected: false, ConnectionId: null);
        }

        if (info.Connected && !string.IsNullOrEmpty(info.ConnectionId))
        {
            return new RefreshRequestResult(Found: true, Connected: true, ConnectionId: info.ConnectionId);
        }

        info.PendingRefresh = true;
        return new RefreshRequestResult(Found: true, Connected: false, ConnectionId: null);
    }

    public IReadOnlyList<ScreenInfo> GetAll()
    {
        PurgeExpired();
        return _screens.Values
            .OrderBy(s => s.Slug)
            .ThenBy(s => s.ConnectedAt)
            .ToList();
    }

    public IReadOnlyList<ScreenInfo> GetBySlug(string slug)
    {
        return _screens.Values
            .Where(s => s.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private ScreenInfo? FindByConnectionId(string connectionId)
    {
        foreach (var info in _screens.Values)
        {
            if (string.Equals(info.ConnectionId, connectionId, StringComparison.Ordinal))
                return info;
        }
        return null;
    }

    private void PurgeExpired()
    {
        var cutoff = DateTime.UtcNow - RetentionWindow;
        foreach (var kvp in _screens)
        {
            var s = kvp.Value;
            if (!s.Connected && s.DisconnectedAt is DateTime disconnectedAt && disconnectedAt < cutoff)
            {
                _screens.TryRemove(kvp.Key, out _);
            }
        }
    }
}

public sealed record RegisterResult(ScreenInfo Screen, IReadOnlyList<string> Evicted, bool HadPendingRefresh);

public sealed record RefreshRequestResult(bool Found, bool Connected, string? ConnectionId);

public sealed class ScreenInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public double Uptime { get; set; }
    public bool IsVisible { get; set; }
    public string? UserAgent { get; set; }
    public string? AppVersion { get; set; }

    // Estado de conexão / retenção
    public bool Connected { get; set; }
    public DateTime? DisconnectedAt { get; set; }
    public bool PendingRefresh { get; set; }
}
