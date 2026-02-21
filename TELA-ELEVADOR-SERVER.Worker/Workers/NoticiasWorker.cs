using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Worker.Workers;

public sealed class NoticiasWorker : BackgroundService
{
    private readonly ILogger<NoticiasWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _intervalMinutes;

    public NoticiasWorker(ILogger<NoticiasWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _intervalMinutes = 120; // 2 horas padrão
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NoticiasWorker iniciado. Intervalo: {IntervalMinutes} minutos", _intervalMinutes);

        // Executar uma vez na inicialização com pequeno delay
        await Task.Delay(5000, stoppingToken);
        await FetchAndStoreNewsAsync(stoppingToken);

        // Executar periodicamente
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_intervalMinutes));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FetchAndStoreNewsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("NoticiasWorker cancelado");
        }
    }

    private async Task FetchAndStoreNewsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Obter todas as fontes de notícias ativas
            var fontes = await dbContext.FontesNoticia
                .Where(f => f.Ativo)
                .ToListAsync(stoppingToken);

            _logger.LogInformation("Buscando notícias de {FonteCount} fonte(s)", fontes.Count);

            foreach (var fonte in fontes)
            {
                try
                {
                    await FetchAndStoreSourceNewsAsync(dbContext, fonte, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao buscar notícias da fonte {FonteNome} - {ErrorMessage}", fonte.Nome, ex.Message);
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Notícias atualizadas com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar notícias");
        }
    }

    private async Task FetchAndStoreSourceNewsAsync(AppDbContext dbContext, FonteNoticia fonte, CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(fonte.UrlBase))
        {
            _logger.LogWarning("FonteNoticia {FonteNome} sem URL", fonte.Nome);
            return;
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        try
        {
            var content = await client.GetStringAsync(fonte.UrlBase, stoppingToken);

            // Parse simplificado de RSS/Atom
            var noticias = ParseRssContent(content, fonte);

            // Verificar duplicatas e adicionar novas
            var contagemAdicionadas = 0;
            foreach (var noticia in noticias)
            {
                var jaExiste = await dbContext.Noticias
                    .AnyAsync(n => n.FonteChave == fonte.Chave && n.Titulo == noticia.Titulo, stoppingToken);

                if (!jaExiste)
                {
                    dbContext.Noticias.Add(noticia);
                    contagemAdicionadas++;
                }
            }

            // Manter apenas últimas 500 notícias por fonte (cleanup)
            var noticiasAntigas = await dbContext.Noticias
                .Where(n => n.FonteChave == fonte.Chave)
                .OrderByDescending(n => n.PublicadoEmUtc)
                .Skip(500)
                .ToListAsync(stoppingToken);

            foreach (var noticia in noticiasAntigas)
            {
                dbContext.Noticias.Remove(noticia);
            }

            _logger.LogInformation("Fonte {FonteNome}: {Adicionadas} notícias adicionadas", fonte.Nome, contagemAdicionadas);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro HTTP ao buscar notícias da fonte {FonteNome}", fonte.Nome);
        }
    }

    private List<Noticia> ParseRssContent(string content, FonteNoticia fonte)
    {
        var noticias = new List<Noticia>();

        try
        {
            // Parse simplificado de XML RSS
            var xmlDoc = new System.Xml.XmlDocument();
            xmlDoc.LoadXml(content);

            var nodes = xmlDoc.SelectNodes("//item");

            foreach (System.Xml.XmlNode node in nodes)
            {
                try
                {
                    var titulo = node.SelectSingleNode("title")?.InnerText?.Trim() ?? "";
                    var descricao = node.SelectSingleNode("description")?.InnerText?.Trim() ?? "";
                    var link = node.SelectSingleNode("link")?.InnerText?.Trim() ?? "";
                    var pubDate = node.SelectSingleNode("pubDate")?.InnerText?.Trim() ?? DateTime.UtcNow.ToString("o");
                    var imagem = node.SelectSingleNode("image/url")?.InnerText?.Trim() ??
                                 node.SelectSingleNode("enclosure")?.Attributes?["url"]?.Value ?? "";

                    if (string.IsNullOrWhiteSpace(titulo))
                        continue;

                    // Tentar fazer parse da data
                    var publicadoEm = DateTime.UtcNow;
                    if (!DateTime.TryParse(pubDate, out var dateParsed))
                    {
                        // Tentar RFC 822
                        try
                        {
                            publicadoEm = DateTime.ParseExact(pubDate, "R", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            publicadoEm = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        publicadoEm = dateParsed;
                    }

                    var noticia = new Noticia
                    {
                        FonteChave = fonte.Chave,
                        FonteNome = fonte.Nome,
                        Titulo = titulo.Length > 500 ? titulo[..500] : titulo,
                        Descricao = descricao.Length > 2000 ? descricao[..2000] : descricao,
                        Link = link.Length > 500 ? link[..500] : link,
                        ImagemUrl = imagem.Length > 500 ? imagem[..500] : imagem,
                        PubDateRaw = pubDate,
                        PublicadoEmUtc = publicadoEm,
                        CriadoEm = DateTime.UtcNow
                    };

                    noticias.Add(noticia);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao fazer parse de item RSS da fonte {FonteNome}", fonte.Nome);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer parse do conteúdo RSS da fonte {FonteNome}", fonte.Nome);
        }

        return noticias;
    }
}
