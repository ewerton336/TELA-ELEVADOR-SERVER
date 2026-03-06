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
    public async Task OnDisconnectedAsync_ShouldUnregisterAndNotify()
    {
        await _hub.JoinPredio("gramado");
        _monitor.GetAll().Should().HaveCount(1);

        await _hub.OnDisconnectedAsync(null);

        _monitor.GetAll().Should().BeEmpty();

        // Should have called Group("monitor") for disconnect notification
        _groupProxy.Verify(p =>
            p.SendCoreAsync("ScreenDisconnected", It.IsAny<object?[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithException_ShouldStillUnregister()
    {
        await _hub.JoinPredio("gramado");

        await _hub.OnDisconnectedAsync(new Exception("connection lost"));

        _monitor.GetAll().Should().BeEmpty();
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
}
