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
                    _logger.LogError(ex, "Erro ao buscar notícias da fonte {FonteNome} - {ErrorMessage}. Tentaremos novamente no próximo intervalo.", fonte.Nome, ex.Message);
                    // Continua processando as próximas fontes mesmo com erro
                }
            }

            try
            {
                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Notícias atualizadas com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar notícias no banco de dados");
                // Não relança a exceção para permitir que o worker continue rodando
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro geral ao buscar notícias. Tentaremos novamente no próximo intervalo.");
        }
    }

    private async Task FetchAndStoreSourceNewsAsync(AppDbContext dbContext, FonteNoticia fonte, CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(fonte.UrlBase))
        {
            _logger.LogWarning("FonteNoticia {FonteNome} sem URL", fonte.Nome);
            return;
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

        _logger.LogInformation("Buscando RSS da fonte {FonteNome} em {Url}", fonte.Nome, fonte.UrlBase);

        try
        {
            var content = await client.GetStringAsync(fonte.UrlBase, stoppingToken);
            _logger.LogDebug("RSS obtido com sucesso de {FonteNome}. Tamanho: {Size} bytes", fonte.Nome, content.Length);

            // Parse simplificado de RSS/Atom
            var noticias = ParseRssContent(content, fonte);
            _logger.LogInformation("Fonte {FonteNome}: {Total} notícias encontradas no RSS", fonte.Nome, noticias.Count);

            // Verificar duplicatas e adicionar novas
            var contagemAdicionadas = 0;
            foreach (var noticia in noticias)
            {
                // Verificar se já existe por Link (que é único no banco)
                var jaExiste = await dbContext.Noticias
                    .AnyAsync(n => n.Link == noticia.Link && !string.IsNullOrEmpty(n.Link), stoppingToken);

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
            _logger.LogWarning(ex, "Fonte {FonteNome} indisponível ({Url}): {Message}. Será reconectada no próximo intervalo.", fonte.Nome, fonte.UrlBase, ex.Message);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Timeout ao buscar notícias da fonte {FonteNome} ({Url}). Será tentada novamente no próximo intervalo.", fonte.Nome, fonte.UrlBase);
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

            if (nodes == null || nodes.Count == 0)
            {
                _logger.LogWarning("Nenhum item RSS encontrado para fonte {FonteNome}. Tamanho do conteúdo: {Size} bytes", fonte.Nome, content.Length);
                return noticias;
            }

            foreach (System.Xml.XmlNode node in nodes)
            {
                try
                {
                    var titulo = node.SelectSingleNode("title")?.InnerText?.Trim() ?? "";
                    var descricao = node.SelectSingleNode("description")?.InnerText?.Trim() ?? "";
                    var link = node.SelectSingleNode("link")?.InnerText?.Trim() ?? "";
                    var pubDate = node.SelectSingleNode("pubDate")?.InnerText?.Trim() ?? DateTime.UtcNow.ToString("o");

                    // Buscar imagem em múltiplos formatos (RSS padrão, media:content do G1, enclosure)
                    var imagem = node.SelectSingleNode("image/url")?.InnerText?.Trim() ??
                                 node.SelectSingleNode("*[local-name()='content']")?.Attributes?["url"]?.Value ??
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
                            // Garantir que seja UTC
                            if (publicadoEm.Kind != DateTimeKind.Utc)
                            {
                                publicadoEm = DateTime.SpecifyKind(publicadoEm, DateTimeKind.Utc);
                            }
                        }
                        catch
                        {
                            publicadoEm = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Garantir que seja UTC
                        publicadoEm = dateParsed.Kind == DateTimeKind.Utc
                            ? dateParsed
                            : DateTime.SpecifyKind(dateParsed, DateTimeKind.Utc);
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
