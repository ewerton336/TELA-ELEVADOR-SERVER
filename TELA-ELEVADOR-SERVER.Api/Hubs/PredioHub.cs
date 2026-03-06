using Microsoft.AspNetCore.SignalR;
using TELA_ELEVADOR_SERVER.Api.Services;

namespace TELA_ELEVADOR_SERVER.Api.Hubs;

public sealed record ScreenHeartbeat(string Slug, double Uptime, bool IsVisible);

public sealed class PredioHub : Hub
{
    private readonly ScreenMonitorService _monitor;

    public PredioHub(ScreenMonitorService monitor)
    {
        _monitor = monitor;
    }

    /// <summary>
    /// Tela do elevador chama ao conectar, informando qual prédio exibe.
    /// </summary>
    public async Task JoinPredio(string slug)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, slug);

        var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();
        _monitor.Register(Context.ConnectionId, slug, userAgent);

        await Clients.Group("monitor")
            .SendAsync("ScreenConnected", new
            {
                Context.ConnectionId,
                Slug = slug,
                ConnectedAt = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Heartbeat periódico da tela (a cada 30s).
    /// </summary>
    public async Task Heartbeat(ScreenHeartbeat data)
    {
        _monitor.UpdateHeartbeat(Context.ConnectionId, data.Uptime, data.IsVisible);

        await Clients.Group("monitor")
            .SendAsync("ScreenHeartbeat", new
            {
                Context.ConnectionId,
                data.Slug,
                data.Uptime,
                data.IsVisible,
                ReceivedAt = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Painel Master se inscreve para receber eventos de monitoramento.
    /// </summary>
    public async Task JoinMonitor()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "monitor");

        // Envia snapshot atual de todas as telas
        var screens = _monitor.GetAll();
        await Clients.Caller.SendAsync("ScreenSnapshot", screens);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _monitor.Unregister(Context.ConnectionId);

        await Clients.Group("monitor")
            .SendAsync("ScreenDisconnected", new
            {
                Context.ConnectionId,
                DisconnectedAt = DateTime.UtcNow
            });

        await base.OnDisconnectedAsync(exception);
    }
}
