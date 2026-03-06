using FluentAssertions;
using TELA_ELEVADOR_SERVER.Application.Noticias;
using TELA_ELEVADOR_SERVER.Infrastructure.Noticias;

namespace TELA_ELEVADOR_SERVER.Tests;

public class NoticiaProviderTests
{
    private static readonly HttpClient SharedClient = new();

    private readonly INoticiaProvider[] _providers =
    {
        new G1NoticiaProvider(SharedClient),
        new DiarioLitoralNoticiaProvider(SharedClient),
        new SantaPortalNoticiaProvider(SharedClient),
    };

    /// <summary>
    /// Teste principal: pelo menos 1 dos 3 providers deve retornar notícias de hoje.
    /// Providers que estiverem fora do ar são ignorados, mas o teste falha
    /// se NENHUM conseguir trazer notícias do dia.
    /// </summary>
    [Fact]
    public async Task Providers_DevemRetornarNoticiasDoDia_EmPeloMenosUm()
    {
        var hoje = DateTime.UtcNow.Date;
        var resultados = new Dictionary<string, (int Total, int DoDia, string? Erro)>();

        foreach (var provider in _providers)
        {
            try
            {
                var noticias = await provider.BuscarUltimasAsync();
                var doDia = noticias.Count(n => ParseDateUtc(n.PubDate)?.Date == hoje);
                resultados[provider.Chave] = (noticias.Count, doDia, null);
            }
            catch (Exception ex)
            {
                resultados[provider.Chave] = (0, 0, ex.Message);
            }
        }

        var totalDoDia = resultados.Values.Sum(r => r.DoDia);
        var resumo = string.Join(" | ", resultados.Select(r =>
            r.Value.Erro != null
                ? $"{r.Key}: ERRO ({r.Value.Erro})"
                : $"{r.Key}: {r.Value.DoDia}/{r.Value.Total} do dia"));

        totalDoDia.Should().BeGreaterThan(0,
            $"nenhum dos 3 providers retornou notícias de hoje ({hoje:dd/MM/yyyy}). {resumo}");
    }

    /// <summary>
    /// Para cada provider que estiver acessível, as notícias retornadas
    /// devem ter PubDate válido (parseável).
    /// </summary>
    [Theory]
    [InlineData(typeof(G1NoticiaProvider))]
    [InlineData(typeof(DiarioLitoralNoticiaProvider))]
    [InlineData(typeof(SantaPortalNoticiaProvider))]
    public async Task Provider_NoticiasDevemTerPubDateValido(Type providerType)
    {
        var provider = _providers.First(p => p.GetType() == providerType);

        List<NoticiaItem> noticias;
        try
        {
            noticias = await provider.BuscarUltimasAsync();
        }
        catch
        {
            // Feed fora do ar — não é erro de lógica, pular
            return;
        }

        if (noticias.Count == 0)
            return;

        foreach (var noticia in noticias)
        {
            var parsed = ParseDateUtc(noticia.PubDate);
            parsed.Should().NotBeNull(
                $"notícia \"{noticia.Title}\" do provider {provider.Chave} tem PubDate inválido: \"{noticia.PubDate}\"");
        }
    }

    private static DateTime? ParseDateUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var dto))
            return dto.UtcDateTime;

        return null;
    }
}
