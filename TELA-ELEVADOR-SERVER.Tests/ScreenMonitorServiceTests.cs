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
    public void Register_MultipleScreens_ShouldTrackAll()
    {
        _sut.Register("conn-1", "gramado", null);
        _sut.Register("conn-2", "gramado", null);
        _sut.Register("conn-3", "canela", null);

        _sut.GetAll().Should().HaveCount(3);
    }

    [Fact]
    public void Unregister_ShouldRemoveScreen()
    {
        _sut.Register("conn-1", "gramado", null);
        _sut.Register("conn-2", "canela", null);

        _sut.Unregister("conn-1");

        var screens = _sut.GetAll();
        screens.Should().HaveCount(1);
        screens[0].ConnectionId.Should().Be("conn-2");
    }

    [Fact]
    public void Unregister_NonExistentId_ShouldNotThrow()
    {
        var act = () => _sut.Unregister("does-not-exist");
        act.Should().NotThrow();
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
        _sut.Register("conn-2", "gramado", null);
        _sut.Register("conn-3", "canela", null);

        var gramado = _sut.GetBySlug("gramado");
        gramado.Should().HaveCount(2);
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
        _sut.Register("conn-c", "canela", null);

        var screens = _sut.GetAll();
        screens[0].Slug.Should().Be("canela");
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
}
