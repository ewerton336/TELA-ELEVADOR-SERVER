using FluentAssertions;
using TELA_ELEVADOR_SERVER.Api.Services;

namespace TELA_ELEVADOR_SERVER.Tests;

public class ScreenMonitorServiceTests
{
    private readonly ScreenMonitorService _sut = new();

    [Fact]
    public void Register_ShouldAddScreen()
    {
        _sut.Register("conn-1", "gramado", "Mozilla/5.0");

        var screens = _sut.GetAll();
        screens.Should().HaveCount(1);
        screens[0].ConnectionId.Should().Be("conn-1");
        screens[0].Slug.Should().Be("gramado");
        screens[0].UserAgent.Should().Be("Mozilla/5.0");
        screens[0].Connected.Should().BeTrue();
    }

    [Fact]
    public void Register_DuplicateConnectionId_ShouldOverwrite()
    {
        _sut.Register("conn-1", "gramado", null);
        _sut.Register("conn-1", "canela", null);

        var screens = _sut.GetAll();
        screens.Should().HaveCount(1);
        screens[0].Slug.Should().Be("canela");
    }

    [Fact]
    public void Register_MultipleScreens_DifferentSlugs_ShouldTrackAll()
    {
        _sut.Register("conn-1", "gramado", null);
        _sut.Register("conn-2", "canela", null);
        _sut.Register("conn-3", "bento", null);

        _sut.GetAll().Should().HaveCount(3);
    }

    [Fact]
    public void UpdateHeartbeat_ShouldUpdateFields()
    {
        _sut.Register("conn-1", "gramado", null);

        _sut.UpdateHeartbeat("conn-1", 120.5, true);

        var screen = _sut.GetAll()[0];
        screen.Uptime.Should().Be(120.5);
        screen.IsVisible.Should().BeTrue();
        screen.LastHeartbeat.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void UpdateHeartbeat_NonExistentId_ShouldNotThrow()
    {
        var act = () => _sut.UpdateHeartbeat("no-conn", 10, false);
        act.Should().NotThrow();
    }

    [Fact]
    public void UpdateHeartbeat_ShouldUpdateMultipleTimes()
    {
        _sut.Register("conn-1", "gramado", null);

        _sut.UpdateHeartbeat("conn-1", 30, true);
        _sut.UpdateHeartbeat("conn-1", 60, false);

        var screen = _sut.GetAll()[0];
        screen.Uptime.Should().Be(60);
        screen.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void GetBySlug_ShouldFilterBySlug()
    {
        _sut.Register("conn-1", "gramado", null);
        _sut.Register("conn-2", "canela", null);

        var gramado = _sut.GetBySlug("gramado");
        gramado.Should().HaveCount(1);
        gramado.Should().AllSatisfy(s => s.Slug.Should().Be("gramado"));

        var canela = _sut.GetBySlug("canela");
        canela.Should().HaveCount(1);
    }

    [Fact]
    public void GetBySlug_ShouldBeCaseInsensitive()
    {
        _sut.Register("conn-1", "gramado", null);

        _sut.GetBySlug("GRAMADO").Should().HaveCount(1);
        _sut.GetBySlug("Gramado").Should().HaveCount(1);
    }

    [Fact]
    public void GetBySlug_NonExistentSlug_ShouldReturnEmpty()
    {
        _sut.Register("conn-1", "gramado", null);

        _sut.GetBySlug("inexistente").Should().BeEmpty();
    }

    [Fact]
    public void GetAll_ShouldReturnOrderedBySlugThenConnectedAt()
    {
        _sut.Register("conn-b", "canela", null);
        _sut.Register("conn-a", "gramado", null);
        _sut.Register("conn-c", "bento", null);

        var screens = _sut.GetAll();
        screens[0].Slug.Should().Be("bento");
        screens[1].Slug.Should().Be("canela");
        screens[2].Slug.Should().Be("gramado");
    }

    [Fact]
    public void Register_ShouldSetInitialUptimeToZero()
    {
        _sut.Register("conn-1", "gramado", null);

        _sut.GetAll()[0].Uptime.Should().Be(0);
    }

    [Fact]
    public void Register_ShouldSetConnectedAtToNow()
    {
        var before = DateTime.UtcNow;
        _sut.Register("conn-1", "gramado", null);
        var after = DateTime.UtcNow;

        var screen = _sut.GetAll()[0];
        screen.ConnectedAt.Should().BeOnOrAfter(before);
        screen.ConnectedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Register_WithAppVersion_ShouldStoreVersion()
    {
        _sut.Register("conn-1", "gramado", null, "2026-03-06T10:00:00.000Z");

        var screen = _sut.GetAll()[0];
        screen.AppVersion.Should().Be("2026-03-06T10:00:00.000Z");
    }

    [Fact]
    public void Register_WithNullAppVersion_ShouldStoreNull()
    {
        _sut.Register("conn-1", "gramado", null);

        var screen = _sut.GetAll()[0];
        screen.AppVersion.Should().BeNull();
    }

    [Fact]
    public void UpdateHeartbeat_WithAppVersion_ShouldUpdateVersion()
    {
        _sut.Register("conn-1", "gramado", null, "2026-03-06T10:00:00.000Z");

        _sut.UpdateHeartbeat("conn-1", 60, true, "2026-03-06T12:00:00.000Z");

        var screen = _sut.GetAll()[0];
        screen.AppVersion.Should().Be("2026-03-06T12:00:00.000Z");
    }

    [Fact]
    public void UpdateHeartbeat_WithNullAppVersion_ShouldKeepExisting()
    {
        _sut.Register("conn-1", "gramado", null, "2026-03-06T10:00:00.000Z");

        _sut.UpdateHeartbeat("conn-1", 60, true);

        var screen = _sut.GetAll()[0];
        screen.AppVersion.Should().Be("2026-03-06T10:00:00.000Z");
    }

    // -------------------------------------------------------
    // Deduplicação por deviceId (mesma tela reconectando)
    // -------------------------------------------------------

    [Fact]
    public void Register_FirstRegistration_ShouldReturnEmptyEvictedList()
    {
        var result = _sut.Register("conn-1", "gramado", null);

        result.Evicted.Should().BeEmpty();
        result.HadPendingRefresh.Should().BeFalse();
    }

    [Fact]
    public void Register_SameDeviceNewConnection_ShouldEvictOldConnection()
    {
        _sut.Register("conn-1", "gramado", null, null, "device-A");
        _sut.Register("conn-2", "gramado", null, null, "device-A");

        var screens = _sut.GetAll();
        screens.Should().HaveCount(1);
        screens[0].ConnectionId.Should().Be("conn-2");
    }

    [Fact]
    public void Register_SameDeviceNewConnection_ShouldReturnEvictedIds()
    {
        _sut.Register("conn-1", "gramado", null, null, "device-A");

        var result = _sut.Register("conn-2", "gramado", null, null, "device-A");

        result.Evicted.Should().ContainSingle().Which.Should().Be("conn-1");
    }

    [Fact]
    public void Register_DifferentDevices_ShouldNotEvictAnything()
    {
        _sut.Register("conn-1", "gramado", null, null, "device-A");

        var result = _sut.Register("conn-2", "gramado", null, null, "device-B");

        result.Evicted.Should().BeEmpty();
        _sut.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void Register_SameConnectionId_ShouldNotEvictItself()
    {
        _sut.Register("conn-1", "gramado", null, null, "device-A");

        var result = _sut.Register("conn-1", "gramado", null, null, "device-A");

        result.Evicted.Should().BeEmpty();
        _sut.GetAll().Should().HaveCount(1);
    }

    [Fact]
    public void Register_SameDeviceReconnect_ShouldNotInheritOldUptime()
    {
        _sut.Register("conn-1", "gramado", "OldBrowser", "v1.0", "device-A");
        _sut.UpdateHeartbeat("conn-1", 3600, true);

        _sut.Register("conn-2", "gramado", "NewBrowser", "v2.0", "device-A");

        var screen = _sut.GetAll()[0];
        screen.ConnectionId.Should().Be("conn-2");
        screen.UserAgent.Should().Be("NewBrowser");
        screen.AppVersion.Should().Be("v2.0");
        screen.Uptime.Should().Be(0); // reinicia, não herda uptime antigo
        screen.Connected.Should().BeTrue();
    }

    [Fact]
    public void Register_EvictedConnection_ShouldNotRespondToHeartbeat()
    {
        _sut.Register("conn-1", "gramado", null, null, "device-A");
        _sut.Register("conn-2", "gramado", null, null, "device-A"); // evicta conn-1

        // Heartbeat do conn-1 antigo não deve causar efeito
        _sut.UpdateHeartbeat("conn-1", 999, true);

        _sut.GetAll().Should().HaveCount(1);
        _sut.GetAll()[0].ConnectionId.Should().Be("conn-2");
        _sut.GetAll()[0].Uptime.Should().Be(0);
    }

    // -------------------------------------------------------
    // Retenção: marca offline e mantém por até 8h
    // -------------------------------------------------------

    [Fact]
    public void MarkDisconnected_ShouldKeepScreenButMarkOffline()
    {
        _sut.Register("conn-1", "gramado", null, null, "device-A");

        var info = _sut.MarkDisconnected("conn-1");

        info.Should().NotBeNull();
        var screens = _sut.GetAll();
        screens.Should().HaveCount(1);
        screens[0].Connected.Should().BeFalse();
        screens[0].DisconnectedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkDisconnected_NonExistentId_ShouldReturnNull()
    {
        var info = _sut.MarkDisconnected("does-not-exist");
        info.Should().BeNull();
    }

    [Fact]
    public void MarkDisconnected_StaleConnectionId_ShouldBeNoOp()
    {
        // Tela reconectou com conn-2 antes do disconnect do conn-1 chegar.
        _sut.Register("conn-1", "gramado", null, null, "device-A");
        _sut.Register("conn-2", "gramado", null, null, "device-A");

        var info = _sut.MarkDisconnected("conn-1"); // connectionId antigo

        info.Should().BeNull(); // não corresponde à sessão atual
        _sut.GetAll()[0].Connected.Should().BeTrue(); // sessão atual continua online
    }

    [Fact]
    public void Reconnect_ShouldClearOfflineState()
    {
        _sut.Register("conn-1", "gramado", null, null, "device-A");
        _sut.MarkDisconnected("conn-1");

        _sut.Register("conn-2", "gramado", null, null, "device-A");

        var screen = _sut.GetAll()[0];
        screen.Connected.Should().BeTrue();
        screen.DisconnectedAt.Should().BeNull();
    }

    [Fact]
    public void GetAll_ShouldPurgeDisconnectedScreensOlderThanRetentionWindow()
    {
        _sut.Register("conn-1", "gramado", null, null, "device-A");
        var info = _sut.MarkDisconnected("conn-1");
        info.Should().NotBeNull();

        // Simula desconexão há mais de 8h
        info!.DisconnectedAt = DateTime.UtcNow - ScreenMonitorService.RetentionWindow - TimeSpan.FromMinutes(1);

        _sut.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void GetAll_ShouldKeepDisconnectedScreensWithinRetentionWindow()
    {
        _sut.Register("conn-1", "gramado", null, null, "device-A");
        _sut.MarkDisconnected("conn-1");

        _sut.GetAll().Should().HaveCount(1);
    }

    // -------------------------------------------------------
    // Refresh pendente (comando para tela offline)
    // -------------------------------------------------------

    [Fact]
    public void RequestRefresh_ConnectedScreen_ShouldReturnConnectionIdAndNotQueue()
    {
        _sut.Register("conn-1", "gramado", null, null, "device-A");

        var target = _sut.RequestRefresh("device-A");

        target.Found.Should().BeTrue();
        target.Connected.Should().BeTrue();
        target.ConnectionId.Should().Be("conn-1");
        _sut.GetAll()[0].PendingRefresh.Should().BeFalse();
    }

    [Fact]
    public void RequestRefresh_OfflineScreen_ShouldQueuePendingRefresh()
    {
        _sut.Register("conn-1", "gramado", null, null, "device-A");
        _sut.MarkDisconnected("conn-1");

        var target = _sut.RequestRefresh("device-A");

        target.Found.Should().BeTrue();
        target.Connected.Should().BeFalse();
        target.ConnectionId.Should().BeNull();
        _sut.GetAll()[0].PendingRefresh.Should().BeTrue();
    }

    [Fact]
    public void RequestRefresh_UnknownDevice_ShouldReturnNotFound()
    {
        var target = _sut.RequestRefresh("ghost-device");

        target.Found.Should().BeFalse();
    }

    [Fact]
    public void Register_AfterPendingRefresh_ShouldReportAndClearIt()
    {
        _sut.Register("conn-1", "gramado", null, null, "device-A");
        _sut.MarkDisconnected("conn-1");
        _sut.RequestRefresh("device-A"); // agenda refresh

        var result = _sut.Register("conn-2", "gramado", null, null, "device-A");

        result.HadPendingRefresh.Should().BeTrue();
        _sut.GetAll()[0].PendingRefresh.Should().BeFalse(); // consumido
    }
}
