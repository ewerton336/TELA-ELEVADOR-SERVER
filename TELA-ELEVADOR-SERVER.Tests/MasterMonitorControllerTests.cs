using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TELA_ELEVADOR_SERVER.Api.Controllers;
using TELA_ELEVADOR_SERVER.Api.Services;

namespace TELA_ELEVADOR_SERVER.Tests;

public class MasterMonitorControllerTests
{
    private readonly ScreenMonitorService _monitor = new();
    private readonly MasterMonitorController _controller;

    public MasterMonitorControllerTests()
    {
        _controller = new MasterMonitorController(_monitor);
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
}
