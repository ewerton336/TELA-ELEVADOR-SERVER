using FluentAssertions;
using TELA_ELEVADOR_SERVER.Domain.Weather;

namespace TELA_ELEVADOR_SERVER.Tests;

public class WeatherCodeTranslatorTests
{
    public static readonly int[] CodigosOpenMeteo =
    {
        0, 1, 2, 3, 45, 48, 51, 53, 55, 56, 57, 61, 63, 65, 66, 67,
        71, 73, 75, 77, 80, 81, 82, 85, 86, 95, 96, 99,
    };

    public static IEnumerable<object[]> CodigosData()
        => CodigosOpenMeteo.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(CodigosData))]
    public void Translate_CodigoWmoDaOpenMeteo_DeveTerDescricaoEIconeConhecidos(int code)
    {
        var (descricao, icone) = WeatherCodeTranslator.Translate(code);

        descricao.Should().NotBe(
            WeatherCodeTranslator.UnknownDescription,
            $"o código WMO {code} pode vir da Open-Meteo e precisa de descrição");
        icone.Should().NotBe(WeatherCodeTranslator.UnknownIcon);
    }

    [Fact]
    public void Translate_CodigoInexistente_DeveRetornarDesconhecido()
    {
        var (descricao, icone) = WeatherCodeTranslator.Translate(12345);

        descricao.Should().Be(WeatherCodeTranslator.UnknownDescription);
        icone.Should().Be(WeatherCodeTranslator.UnknownIcon);
    }
}
