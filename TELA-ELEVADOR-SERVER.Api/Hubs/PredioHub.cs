using Microsoft.AspNetCore.SignalR;
using TELA_ELEVADOR_SERVER.Api.Services;

namespace TELA_ELEVADOR_SERVER.Api.Hubs;

public sealed record ScreenHeartbeat(string Slug, double Uptime, bool IsVisible, string? AppVersion = null);

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
    public async Task JoinPredio(string slug, string? appVersion = null, string? deviceId = null)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, slug);

        var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();
        var result = _monitor.Register(Context.ConnectionId, slug, userAgent, appVersion, deviceId);

        // Remove conexões antigas (evictas) do grupo SignalR
        foreach (var oldConnId in result.Evicted)
        {
            await Groups.RemoveFromGroupAsync(oldConnId, slug);
        }

        await Clients.Group("monitor")
            .SendAsync("ScreenConnected", new
            {
                Context.ConnectionId,
                result.Screen.DeviceId,
                Slug = slug,
                ConnectedAt = DateTime.UtcNow,
                AppVersion = appVersion
            });

        // Havia um comando de atualização agendado enquanto a tela estava
        // offline — entrega agora, fazendo a tela recarregar.
        if (result.HadPendingRefresh)
        {
            await Clients.Caller.SendAsync("ForceRefresh");
        }
    }

    /// <summary>
    /// Heartbeat periódico da tela (a cada 30s).
    /// </summary>
    public async Task Heartbeat(ScreenHeartbeat data)
    {
        _monitor.UpdateHeartbeat(Context.ConnectionId, data.Uptime, data.IsVisible, data.AppVersion);

        await Clients.Group("monitor")
            .SendAsync("ScreenHeartbeat", new
            {
                Context.ConnectionId,
                data.Slug,
                data.Uptime,
                data.IsVisible,
                data.AppVersion,
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
        // Não remove a tela — apenas marca como desconectada. Os dados ficam
        // retidos para que o admin ainda a veja e possa agendar uma atualização.
        var info = _monitor.MarkDisconnected(Context.ConnectionId);

        if (info is not null)
        {
            await Clients.Group("monitor")
                .SendAsync("ScreenDisconnected", new
                {
                    Context.ConnectionId,
                    info.DeviceId,
                    info.Slug,
                    DisconnectedAt = DateTime.UtcNow
                });
        }

        await base.OnDisconnectedAsync(exception);
    }
}
