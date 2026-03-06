using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using TELA_ELEVADOR_SERVER.Api.Controllers;
using TELA_ELEVADOR_SERVER.Api.Hubs;
using TELA_ELEVADOR_SERVER.Api.Services;

namespace TELA_ELEVADOR_SERVER.Tests;

public class MasterMonitorControllerTests
{
    private readonly ScreenMonitorService _monitor = new();
    private readonly Mock<IHubContext<PredioHub>> _hubContext = new();
    private readonly Mock<IHubClients> _hubClients = new();
    private readonly Mock<ISingleClientProxy> _clientProxy = new();
    private readonly MasterMonitorController _controller;

    public MasterMonitorControllerTests()
    {
        _hubClients.Setup(c => c.Client(It.IsAny<string>())).Returns(_clientProxy.Object);
        _hubContext.Setup(h => h.Clients).Returns(_hubClients.Object);
        _controller = new MasterMonitorController(_monitor, _hubContext.Object);
    }

    [Fact]
    public void GetScreens_NoScreens_ShouldReturnEmptyList()
    {
        var result = _controller.GetScreens();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var screens = okResult.Value.Should().BeAssignableTo<IReadOnlyList<ScreenInfo>>().Subject;
        screens.Should().BeEmpty();
    }

    [Fact]
    public void GetScreens_WithScreens_ShouldReturnAll()
    {
        _monitor.Register("conn-1", "gramado", "Agent1");
        _monitor.Register("conn-2", "canela", "Agent2");

        var result = _controller.GetScreens();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var screens = okResult.Value.Should().BeAssignableTo<IReadOnlyList<ScreenInfo>>().Subject;
        screens.Should().HaveCount(2);
    }

    [Fact]
    public void GetScreens_ShouldReflectHeartbeatUpdates()
    {
        _monitor.Register("conn-1", "gramado", null);
        _monitor.UpdateHeartbeat("conn-1", 300, true);

        var result = _controller.GetScreens();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var screens = okResult.Value.Should().BeAssignableTo<IReadOnlyList<ScreenInfo>>().Subject;
        screens[0].Uptime.Should().Be(300);
        screens[0].IsVisible.Should().BeTrue();
    }

    [Fact]
    public void GetScreens_AfterUnregister_ShouldNotIncludeRemoved()
    {
        _monitor.Register("conn-1", "gramado", null);
        _monitor.Register("conn-2", "canela", null);
        _monitor.Unregister("conn-1");

        var result = _controller.GetScreens();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var screens = okResult.Value.Should().BeAssignableTo<IReadOnlyList<ScreenInfo>>().Subject;
        screens.Should().HaveCount(1);
        screens[0].Slug.Should().Be("canela");
    }

    [Fact]
    public async Task ForceRefresh_ValidConnectionId_ShouldSendToClient()
    {
        var request = new ForceRefreshRequest("conn-123");

        var result = await _controller.ForceRefresh(request);

        result.Should().BeOfType<OkObjectResult>();
        _hubClients.Verify(c => c.Client("conn-123"), Times.Once);
        _clientProxy.Verify(p =>
            p.SendCoreAsync("ForceRefresh", It.Is<object?[]>(a => a.Length == 0), default),
            Times.Once);
    }

    [Fact]
    public async Task ForceRefresh_EmptyConnectionId_ShouldReturnBadRequest()
    {
        var request = new ForceRefreshRequest("");

        var result = await _controller.ForceRefresh(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ForceRefresh_WhitespaceConnectionId_ShouldReturnBadRequest()
    {
        var request = new ForceRefreshRequest("   ");

        var result = await _controller.ForceRefresh(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
