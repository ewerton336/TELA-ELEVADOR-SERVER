using System.Collections.Concurrent;

namespace TELA_ELEVADOR_SERVER.Api.Services;

public sealed class ScreenMonitorService
{
    private readonly ConcurrentDictionary<string, ScreenInfo> _screens = new();

    public void Register(string connectionId, string slug, string? userAgent, string? appVersion = null)
    {
        _screens[connectionId] = new ScreenInfo
        {
            ConnectionId = connectionId,
            Slug = slug,
            ConnectedAt = DateTime.UtcNow,
            LastHeartbeat = DateTime.UtcNow,
            UserAgent = userAgent,
            AppVersion = appVersion
        };
    }

    public void UpdateHeartbeat(string connectionId, double uptime, bool isVisible, string? appVersion = null)
    {
        if (_screens.TryGetValue(connectionId, out var info))
        {
            info.LastHeartbeat = DateTime.UtcNow;
            info.Uptime = uptime;
            info.IsVisible = isVisible;
            if (appVersion is not null)
                info.AppVersion = appVersion;
        }
    }

    public void Unregister(string connectionId)
    {
        _screens.TryRemove(connectionId, out _);
    }

    public IReadOnlyList<ScreenInfo> GetAll()
    {
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
}

public sealed class ScreenInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public double Uptime { get; set; }
    public bool IsVisible { get; set; }
    public string? UserAgent { get; set; }
    public string? AppVersion { get; set; }
}
