using FluentAssertions;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.Api.Controllers;

namespace TELA_ELEVADOR_SERVER.Tests;

public class PredioModulesTests
{
    // ── Predio entity defaults ──

    [Fact]
    public void NovoPredio_TodosModulosDevemSerTrue()
    {
        var predio = new Predio();

        predio.ModuloBuildingNotice.Should().BeTrue();
        predio.ModuloWeather.Should().BeTrue();
        predio.ModuloHeadlineNews.Should().BeTrue();
        predio.ModuloNewsTicker.Should().BeTrue();
    }

    [Fact]
    public void Predio_DevePermitirDesativarModulosIndividualmente()
    {
        var predio = new Predio
        {
            ModuloBuildingNotice = false,
            ModuloWeather = true,
            ModuloHeadlineNews = false,
            ModuloNewsTicker = true,
        };

        predio.ModuloBuildingNotice.Should().BeFalse();
        predio.ModuloWeather.Should().BeTrue();
        predio.ModuloHeadlineNews.Should().BeFalse();
        predio.ModuloNewsTicker.Should().BeTrue();
    }

    [Fact]
    public void Predio_DevePermitirDesativarTodosModulos()
    {
        var predio = new Predio
        {
            ModuloBuildingNotice = false,
            ModuloWeather = false,
            ModuloHeadlineNews = false,
            ModuloNewsTicker = false,
        };

        predio.ModuloBuildingNotice.Should().BeFalse();
        predio.ModuloWeather.Should().BeFalse();
        predio.ModuloHeadlineNews.Should().BeFalse();
        predio.ModuloNewsTicker.Should().BeFalse();
    }

    // ── Record types ──

    [Fact]
    public void ScreenModulesRequest_DeveCriarComValoresCorretos()
    {
        var request = new AdminPredioController.ScreenModulesRequest(
            BuildingNotice: true,
            Weather: false,
            HeadlineNews: true,
            NewsTicker: false);

        request.BuildingNotice.Should().BeTrue();
        request.Weather.Should().BeFalse();
        request.HeadlineNews.Should().BeTrue();
        request.NewsTicker.Should().BeFalse();
    }

    [Fact]
    public void ScreenModulesResponse_DeveCriarComValoresCorretos()
    {
        var response = new AdminPredioController.ScreenModulesResponse(
            BuildingNotice: false,
            Weather: true,
            HeadlineNews: false,
            NewsTicker: true);

        response.BuildingNotice.Should().BeFalse();
        response.Weather.Should().BeTrue();
        response.HeadlineNews.Should().BeFalse();
        response.NewsTicker.Should().BeTrue();
    }

    [Fact]
    public void ScreenModulesRequest_TodosTrue_DeveSerIgualADefault()
    {
        var a = new AdminPredioController.ScreenModulesRequest(true, true, true, true);
        var b = new AdminPredioController.ScreenModulesRequest(true, true, true, true);

        a.Should().Be(b);
    }

    [Fact]
    public void ScreenModulesRequest_Diferentes_NaoDevemSerIguais()
    {
        var a = new AdminPredioController.ScreenModulesRequest(true, true, true, true);
        var b = new AdminPredioController.ScreenModulesRequest(true, false, true, true);

        a.Should().NotBe(b);
    }

    [Fact]
    public void ScreenModulesResponse_Igualdade_DeveUsarValueEquality()
    {
        var a = new AdminPredioController.ScreenModulesResponse(true, false, true, false);
        var b = new AdminPredioController.ScreenModulesResponse(true, false, true, false);

        (a == b).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Predio_ModulosNaoDevemAfetarOutrasPropriedades()
    {
        var predio = new Predio
        {
            Slug = "gramado",
            Nome = "Residencial Gramado",
            OrientationMode = "portrait",
            ModuloBuildingNotice = false,
        };

        predio.Slug.Should().Be("gramado");
        predio.Nome.Should().Be("Residencial Gramado");
        predio.OrientationMode.Should().Be("portrait");
        predio.ModuloBuildingNotice.Should().BeFalse();
        predio.ModuloWeather.Should().BeTrue(); // default
    }
}
