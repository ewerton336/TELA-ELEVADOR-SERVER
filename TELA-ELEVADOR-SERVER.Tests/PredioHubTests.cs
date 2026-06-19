using FluentAssertions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Moq;
using TELA_ELEVADOR_SERVER.Api.Hubs;
using TELA_ELEVADOR_SERVER.Api.Services;

namespace TELA_ELEVADOR_SERVER.Tests;

public class PredioHubTests
{
    private readonly ScreenMonitorService _monitor = new();
    private readonly PredioHub _hub;
    private readonly Mock<IGroupManager> _groups = new();
    private readonly Mock<IHubCallerClients> _clients = new();
    private readonly Mock<ISingleClientProxy> _callerProxy = new();
    private readonly Mock<ISingleClientProxy> _groupProxy = new();
    private const string TestConnectionId = "test-conn-1";

    public PredioHubTests()
    {
        _hub = new PredioHub(_monitor);

        var mockContext = new Mock<HubCallerContext>();
        mockContext.Setup(c => c.ConnectionId).Returns(TestConnectionId);
        // Provide empty FeatureCollection so GetHttpContext() returns null safely
        mockContext.Setup(c => c.Features).Returns(new FeatureCollection());

        _clients.Setup(c => c.Caller).Returns(_callerProxy.Object);
        _clients.Setup(c => c.Group(It.IsAny<string>())).Returns(_groupProxy.Object);

        _hub.Context = mockContext.Object;
        _hub.Clients = _clients.Object;
        _hub.Groups = _groups.Object;
    }

    [Fact]
    public async Task JoinPredio_ShouldAddToGroupAndRegisterScreen()
    {
        await _hub.JoinPredio("gramado");

        _groups.Verify(g =>
            g.AddToGroupAsync(TestConnectionId, "gramado", default),
            Times.Once);

        var screens = _monitor.GetAll();
        screens.Should().HaveCount(1);
        screens[0].Slug.Should().Be("gramado");
        screens[0].ConnectionId.Should().Be(TestConnectionId);
    }

    [Fact]
    public async Task JoinPredio_ShouldNotifyMonitorGroup()
    {
        await _hub.JoinPredio("gramado");

        _clients.Verify(c => c.Group("monitor"), Times.Once);
        _groupProxy.Verify(p =>
            p.SendCoreAsync("ScreenConnected", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task Heartbeat_ShouldUpdateMonitorAndNotify()
    {
        // First register the screen
        await _hub.JoinPredio("gramado");

        var heartbeat = new ScreenHeartbeat("gramado", 45.5, true);
        await _hub.Heartbeat(heartbeat);

        var screen = _monitor.GetAll()[0];
        screen.Uptime.Should().Be(45.5);
        screen.IsVisible.Should().BeTrue();

        // Should have notified "monitor" group (once for JoinPredio, once for Heartbeat)
        _clients.Verify(c => c.Group("monitor"), Times.Exactly(2));
    }

    [Fact]
    public async Task JoinMonitor_ShouldAddToMonitorGroupAndSendSnapshot()
    {
        // Register some screens first
        _monitor.Register("other-conn", "canela", null);

        await _hub.JoinMonitor();

        _groups.Verify(g =>
            g.AddToGroupAsync(TestConnectionId, "monitor", default),
            Times.Once);

        _callerProxy.Verify(p =>
            p.SendCoreAsync("ScreenSnapshot", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldMarkOfflineAndNotify()
    {
        await _hub.JoinPredio("gramado");
        _monitor.GetAll().Should().HaveCount(1);

        await _hub.OnDisconnectedAsync(null);

        // A tela não é removida — apenas marcada como offline (retenção 8h).
        var screens = _monitor.GetAll();
        screens.Should().HaveCount(1);
        screens[0].Connected.Should().BeFalse();
        screens[0].DisconnectedAt.Should().NotBeNull();

        // Should have called Group("monitor") for disconnect notification
        _groupProxy.Verify(p =>
            p.SendCoreAsync("ScreenDisconnected", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithException_ShouldStillMarkOffline()
    {
        await _hub.JoinPredio("gramado");

        await _hub.OnDisconnectedAsync(new Exception("connection lost"));

        var screens = _monitor.GetAll();
        screens.Should().HaveCount(1);
        screens[0].Connected.Should().BeFalse();
    }

    [Fact]
    public async Task JoinPredio_WithAppVersion_ShouldRegisterWithVersion()
    {
        await _hub.JoinPredio("gramado", "2026-03-06T10:00:00.000Z");

        var screens = _monitor.GetAll();
        screens.Should().HaveCount(1);
        screens[0].AppVersion.Should().Be("2026-03-06T10:00:00.000Z");
    }

    [Fact]
    public async Task JoinPredio_WithoutAppVersion_ShouldStillWork()
    {
        await _hub.JoinPredio("gramado");

        var screens = _monitor.GetAll();
        screens.Should().HaveCount(1);
        screens[0].AppVersion.Should().BeNull();
    }

    [Fact]
    public async Task Heartbeat_WithAppVersion_ShouldUpdateVersion()
    {
        await _hub.JoinPredio("gramado", "2026-03-06T10:00:00.000Z");

        var heartbeat = new ScreenHeartbeat("gramado", 60, true, "2026-03-06T12:00:00.000Z");
        await _hub.Heartbeat(heartbeat);

        var screen = _monitor.GetAll()[0];
        screen.AppVersion.Should().Be("2026-03-06T12:00:00.000Z");
    }

    [Fact]
    public async Task Heartbeat_WithAppVersion_ShouldForwardToMonitor()
    {
        await _hub.JoinPredio("gramado", "2026-03-06T10:00:00.000Z");

        var heartbeat = new ScreenHeartbeat("gramado", 45.5, true, "2026-03-06T10:00:00.000Z");
        await _hub.Heartbeat(heartbeat);

        // Should have notified "monitor" group (once for JoinPredio, once for Heartbeat)
        _clients.Verify(c => c.Group("monitor"), Times.Exactly(2));
        _groupProxy.Verify(p =>
            p.SendCoreAsync("ScreenHeartbeat", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    // -------------------------------------------------------
    // Testes de sessão duplicada (mesma tela/deviceId reconectando)
    // -------------------------------------------------------

    [Fact]
    public async Task JoinPredio_SameDeviceNewConnection_ShouldEvictOldAndRemoveFromGroup()
    {
        // Simula: a mesma tela (deviceId = TestConnectionId, pois o hub passa
        // deviceId nulo) já estava registrada com um connectionId antigo.
        _monitor.Register("old-conn", "gramado", null, null, TestConnectionId);

        // Hub (com test-conn-1) faz JoinPredio — mesma tela, nova conexão
        await _hub.JoinPredio("gramado");

        // Deve ter apenas 1 sessão ativa
        var screens = _monitor.GetAll();
        screens.Should().HaveCount(1);
        screens[0].ConnectionId.Should().Be(TestConnectionId);

        // Deve tentar remover a conexão antiga do grupo SignalR
        _groups.Verify(g =>
            g.RemoveFromGroupAsync("old-conn", "gramado", default),
            Times.Once);
    }

    [Fact]
    public async Task JoinPredio_DifferentDevice_ShouldNotEvictOtherScreens()
    {
        // Registra tela de outro dispositivo/prédio
        _monitor.Register("other-conn", "canela", null);

        // Hub se conecta a "gramado"
        await _hub.JoinPredio("gramado");

        // Ambas devem existir
        _monitor.GetAll().Should().HaveCount(2);

        // Não deve remover nada do grupo
        _groups.Verify(g =>
            g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task JoinPredio_CalledTwiceSameConnection_ShouldNotDuplicate()
    {
        await _hub.JoinPredio("gramado");
        await _hub.JoinPredio("gramado");

        _monitor.GetAll().Should().HaveCount(1);
        _monitor.GetAll()[0].ConnectionId.Should().Be(TestConnectionId);
    }

    [Fact]
    public async Task JoinPredio_WithPendingRefresh_ShouldSendForceRefreshToCaller()
    {
        // Tela ficou offline com um refresh agendado.
        _monitor.Register("old-conn", "gramado", null, null, TestConnectionId);
        _monitor.MarkDisconnected("old-conn");
        _monitor.RequestRefresh(TestConnectionId);

        // Ao reconectar, o comando pendente é entregue ao próprio chamador.
        await _hub.JoinPredio("gramado", null, TestConnectionId);

        _callerProxy.Verify(p =>
            p.SendCoreAsync("ForceRefresh", It.Is<object?[]>(a => a.Length == 0), default),
            Times.Once);
    }

    [Fact]
    public async Task JoinPredio_WithoutPendingRefresh_ShouldNotSendForceRefresh()
    {
        await _hub.JoinPredio("gramado");

        _callerProxy.Verify(p =>
            p.SendCoreAsync("ForceRefresh", It.IsAny<object?[]>(), default),
            Times.Never);
    }
}
