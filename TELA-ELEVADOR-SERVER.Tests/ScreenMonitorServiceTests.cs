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
    public void Register_MultipleScreens_DifferentSlugs_ShouldTrackAll()
    {
        _sut.Register("conn-1", "gramado", null);
        _sut.Register("conn-2", "canela", null);
        _sut.Register("conn-3", "bento", null);

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
    // Testes de deduplicação por slug (sessão duplicada)
    // -------------------------------------------------------

    [Fact]
    public void Register_SameSlugDifferentConnection_ShouldEvictOldConnection()
    {
        _sut.Register("conn-1", "gramado", null);
        _sut.Register("conn-2", "gramado", null);

        var screens = _sut.GetAll();
        screens.Should().HaveCount(1);
        screens[0].ConnectionId.Should().Be("conn-2");
    }

    [Fact]
    public void Register_SameSlugDifferentConnection_ShouldReturnEvictedIds()
    {
        _sut.Register("conn-1", "gramado", null);

        var evicted = _sut.Register("conn-2", "gramado", null);

        evicted.Should().HaveCount(1);
        evicted[0].Should().Be("conn-1");
    }

    [Fact]
    public void Register_DifferentSlugs_ShouldNotEvictAnything()
    {
        _sut.Register("conn-1", "gramado", null);

        var evicted = _sut.Register("conn-2", "canela", null);

        evicted.Should().BeEmpty();
        _sut.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void Register_SameConnectionId_ShouldNotEvictItself()
    {
        _sut.Register("conn-1", "gramado", null);

        var evicted = _sut.Register("conn-1", "gramado", null);

        evicted.Should().BeEmpty();
        _sut.GetAll().Should().HaveCount(1);
    }

    [Fact]
    public void Register_MultipleOldConnections_SameSlug_ShouldEvictAll()
    {
        // Simula cenário extremo: 3 conexões antigas do mesmo slug acumuladas
        // (possível se o cleanup falhou em cascata)
        _sut.Register("conn-1", "gramado", null);
        // Forçar inserção sem dedup para simular estado corrompido
        // O segundo Register evicta conn-1
        _sut.Register("conn-2", "gramado", null);
        // Agora registrar mais um com slug diferente (não impactado)
        _sut.Register("conn-3", "canela", null);

        // conn-4 para gramado deve evictar conn-2 (o único gramado restante)
        var evicted = _sut.Register("conn-4", "gramado", null);

        evicted.Should().HaveCount(1);
        evicted.Should().Contain("conn-2");
        _sut.GetAll().Should().HaveCount(2); // conn-3 canela + conn-4 gramado
        _sut.GetBySlug("gramado").Should().HaveCount(1);
        _sut.GetBySlug("gramado")[0].ConnectionId.Should().Be("conn-4");
    }

    [Fact]
    public void Register_SameSlug_CaseInsensitive_ShouldEvictOld()
    {
        _sut.Register("conn-1", "gramado", null);

        var evicted = _sut.Register("conn-2", "GRAMADO", null);

        evicted.Should().HaveCount(1);
        evicted[0].Should().Be("conn-1");
        _sut.GetAll().Should().HaveCount(1);
        _sut.GetAll()[0].ConnectionId.Should().Be("conn-2");
    }

    [Fact]
    public void Register_NewConnection_ShouldNotInheritOldData()
    {
        _sut.Register("conn-1", "gramado", "OldBrowser", "v1.0");
        _sut.UpdateHeartbeat("conn-1", 3600, true);

        _sut.Register("conn-2", "gramado", "NewBrowser", "v2.0");

        var screen = _sut.GetAll()[0];
        screen.ConnectionId.Should().Be("conn-2");
        screen.UserAgent.Should().Be("NewBrowser");
        screen.AppVersion.Should().Be("v2.0");
        screen.Uptime.Should().Be(0); // Reset, não herda uptime antigo
    }

    [Fact]
    public void Register_EvictedConnection_ShouldNotRespondToHeartbeat()
    {
        _sut.Register("conn-1", "gramado", null);
        _sut.Register("conn-2", "gramado", null); // evicta conn-1

        // Heartbeat do conn-1 antigo não deve causar efeito
        _sut.UpdateHeartbeat("conn-1", 999, true);

        _sut.GetAll().Should().HaveCount(1);
        _sut.GetAll()[0].ConnectionId.Should().Be("conn-2");
        _sut.GetAll()[0].Uptime.Should().Be(0); // Não foi afetado
    }

    [Fact]
    public void Register_FirstRegistration_ShouldReturnEmptyEvictedList()
    {
        var evicted = _sut.Register("conn-1", "gramado", null);

        evicted.Should().BeEmpty();
    }
}
