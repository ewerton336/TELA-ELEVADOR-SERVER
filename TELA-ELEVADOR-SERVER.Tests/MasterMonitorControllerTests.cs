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
    private readonly ScreenshotService _screenshots = new();
    private readonly ScreenDetailsService _details = new();
    private readonly Mock<IHubContext<PredioHub>> _hubContext = new();
    private readonly Mock<IHubClients> _hubClients = new();
    private readonly Mock<ISingleClientProxy> _clientProxy = new();
    private readonly MasterMonitorController _controller;

    public MasterMonitorControllerTests()
    {
        _hubClients.Setup(c => c.Client(It.IsAny<string>())).Returns(_clientProxy.Object);
        _hubContext.Setup(h => h.Clients).Returns(_hubClients.Object);
        _controller = new MasterMonitorController(_monitor, _screenshots, _details, _hubContext.Object);
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
    public void GetScreens_AfterDisconnect_ShouldStillIncludeOfflineScreen()
    {
        _monitor.Register("conn-1", "gramado", null, null, "device-1");
        _monitor.Register("conn-2", "canela", null, null, "device-2");
        _monitor.MarkDisconnected("conn-1");

        var result = _controller.GetScreens();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var screens = okResult.Value.Should().BeAssignableTo<IReadOnlyList<ScreenInfo>>().Subject;
        // A tela desconectada continua na lista (offline) — retenção de 8h.
        screens.Should().HaveCount(2);
        screens.Should().Contain(s => s.Slug == "gramado" && !s.Connected);
        screens.Should().Contain(s => s.Slug == "canela" && s.Connected);
    }

    [Fact]
    public async Task ForceRefresh_OnlineScreen_ShouldSendToCurrentConnection()
    {
        _monitor.Register("conn-123", "gramado", null, null, "device-1");
        var request = new ForceRefreshRequest("device-1");

        var result = await _controller.ForceRefresh(request);

        result.Should().BeOfType<OkObjectResult>();
        _hubClients.Verify(c => c.Client("conn-123"), Times.Once);
        _clientProxy.Verify(p =>
            p.SendCoreAsync("ForceRefresh", It.Is<object?[]>(a => a.Length == 0), default),
            Times.Once);
    }

    [Fact]
    public async Task ForceRefresh_OfflineScreen_ShouldQueueWithoutSending()
    {
        _monitor.Register("conn-123", "gramado", null, null, "device-1");
        _monitor.MarkDisconnected("conn-123");
        var request = new ForceRefreshRequest("device-1");

        var result = await _controller.ForceRefresh(request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { message = "Tela offline — atualização agendada para a próxima reconexão.", queued = true });

        // Não deve tentar enviar para nenhum cliente (a tela está offline).
        _hubClients.Verify(c => c.Client(It.IsAny<string>()), Times.Never);
        _monitor.GetAll()[0].PendingRefresh.Should().BeTrue();
    }

    [Fact]
    public async Task ForceRefresh_UnknownDevice_ShouldReturnNotFound()
    {
        var request = new ForceRefreshRequest("ghost-device");

        var result = await _controller.ForceRefresh(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ForceRefresh_EmptyDeviceId_ShouldReturnBadRequest()
    {
        var request = new ForceRefreshRequest("");

        var result = await _controller.ForceRefresh(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ForceRefresh_WhitespaceDeviceId_ShouldReturnBadRequest()
    {
        var request = new ForceRefreshRequest("   ");

        var result = await _controller.ForceRefresh(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Print (screenshot) — agora agendável quando offline ──

    [Fact]
    public async Task RequestScreenshot_OnlineScreen_ShouldSendToCurrentConnection()
    {
        _monitor.Register("conn-ss", "gramado", null, null, "device-ss");
        var request = new ForceRefreshRequest("device-ss");

        var result = await _controller.RequestScreenshot(request);

        result.Should().BeOfType<OkObjectResult>();
        _clientProxy.Verify(p =>
            p.SendCoreAsync("RequestScreenshot", It.Is<object?[]>(a => a.Length == 0), default),
            Times.Once);
        _monitor.GetAll()[0].PendingScreenshot.Should().BeFalse();
    }

    [Fact]
    public async Task RequestScreenshot_OfflineScreen_ShouldQueueWithoutSending()
    {
        _monitor.Register("conn-ss", "gramado", null, null, "device-ss");
        _monitor.MarkDisconnected("conn-ss");
        var request = new ForceRefreshRequest("device-ss");

        var result = await _controller.RequestScreenshot(request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { message = "Tela offline — print agendado para a próxima reconexão.", queued = true });
        _hubClients.Verify(c => c.Client(It.IsAny<string>()), Times.Never);
        _monitor.GetAll()[0].PendingScreenshot.Should().BeTrue();
    }

    [Fact]
    public async Task RequestScreenshot_UnknownDevice_ShouldReturnNotFound()
    {
        var request = new ForceRefreshRequest("ghost");

        var result = await _controller.RequestScreenshot(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Detalhes da tela — sob demanda e agendável quando offline ──

    [Fact]
    public async Task RequestDetails_OnlineScreen_ShouldSendToCurrentConnection()
    {
        _monitor.Register("conn-d", "gramado", null, null, "device-d");
        var request = new ForceRefreshRequest("device-d");

        var result = await _controller.RequestDetails(request);

        result.Should().BeOfType<OkObjectResult>();
        _clientProxy.Verify(p =>
            p.SendCoreAsync("RequestScreenDetails", It.Is<object?[]>(a => a.Length == 0), default),
            Times.Once);
        _monitor.GetAll()[0].PendingDetails.Should().BeFalse();
    }

    [Fact]
    public async Task RequestDetails_OfflineScreen_ShouldQueueWithoutSending()
    {
        _monitor.Register("conn-d", "gramado", null, null, "device-d");
        _monitor.MarkDisconnected("conn-d");
        var request = new ForceRefreshRequest("device-d");

        var result = await _controller.RequestDetails(request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { message = "Tela offline — detalhes agendados para a próxima reconexão.", queued = true });
        _hubClients.Verify(c => c.Client(It.IsAny<string>()), Times.Never);
        _monitor.GetAll()[0].PendingDetails.Should().BeTrue();
    }

    [Fact]
    public async Task RequestDetails_EmptyDeviceId_ShouldReturnBadRequest()
    {
        var request = new ForceRefreshRequest("");

        var result = await _controller.RequestDetails(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void GetCaptures_AfterSavingShotAndDetails_ShouldReturnTimestamps()
    {
        _screenshots.Save("device-x", new byte[] { 1, 2, 3 }, "image/png");
        _details.Save("device-y", "{\"a\":1}");

        var result = _controller.GetCaptures();

        result.Should().BeOfType<OkObjectResult>();
    }
}
