using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using TELA_ELEVADOR_SERVER.Infrastructure.Services;

namespace TELA_ELEVADOR_SERVER.Tests;

public class OpenMeteoGeocodingServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _json;
        private readonly HttpStatusCode _status;

        public StubHandler(string json, HttpStatusCode status = HttpStatusCode.OK)
        {
            _json = json;
            _status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            });
    }

    private static OpenMeteoGeocodingService ServiceComResposta(string json)
        => new(new HttpClient(new StubHandler(json)));

    [Fact]
    public async Task GeocodeAsync_ComVariosResultados_DevePreferirSaoPaulo()
    {
        const string json = """
        {"results":[
          {"name":"Praia Grande","latitude":-29.19667,"longitude":-49.95028,"admin1":"Santa Catarina","country_code":"BR"},
          {"name":"Praia Grande","latitude":-24.00583,"longitude":-46.40278,"admin1":"São Paulo","country_code":"BR"}
        ]}
        """;

        var service = ServiceComResposta(json);

        var result = await service.GeocodeAsync("Praia Grande");

        result.Should().NotBeNull();
        result!.Latitude.Should().BeApproximately(-24.00583, 0.0001);
        result.Longitude.Should().BeApproximately(-46.40278, 0.0001);
    }

    [Fact]
    public async Task GeocodeAsync_SemResultados_DeveRetornarNull()
    {
        const string json = """{"generationtime_ms":0.5}""";

        var service = ServiceComResposta(json);

        var result = await service.GeocodeAsync("Cidade Inexistente XYZ");

        result.Should().BeNull();
    }
}
