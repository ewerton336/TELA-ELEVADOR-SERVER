using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Application.Noticias;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Noticias;

public sealed class NoticiaService : INoticiaService
{
    private const int MaxTake = 50;
    private readonly AppDbContext _dbContext;

    public NoticiaService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<NoticiaItem>> BuscarNoticiasAsync(IEnumerable<string> chaves,int take = 30)
    {
        var enabled = chaves
            .Where(chave => !string.IsNullOrWhiteSpace(chave))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (enabled.Count == 0)
        {
            return new List<NoticiaItem>();
        }

        var normalizedTake = Math.Clamp(take, 1, MaxTake);
        
        // Buscar mais notícias para ter buffer suficiente de cada fonte
        var bufferMultiplier = Math.Max(3, enabled.Count);
        var fetchCount = normalizedTake * bufferMultiplier;

        var noticias = await _dbContext.Noticias
            .AsNoTracking()
            .Where(n => enabled.Contains(n.FonteChave))
            .OrderByDescending(n => n.PublicadoEmUtc)
            .Take(fetchCount)
            .ToListAsync();

        // Agrupar por fonte mantendo ordem por data dentro de cada grupo
        var porFonte = noticias
            .GroupBy(n => n.FonteChave)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(n => n.PublicadoEmUtc).ToList()
            );

        // Aplicar interleaving (round-robin entre fontes)
        var noticiasIntercaladas = InterleaveBySource(porFonte, normalizedTake);

        return noticiasIntercaladas.Select(n => new NoticiaItem(
                n.Link,
                n.Titulo,
                TrimToNextPunctuation(n.Descricao, 200),
                n.Link,
                n.ImagemUrl,
                string.IsNullOrWhiteSpace(n.PubDateRaw) ? n.PublicadoEmUtc.ToString("R", CultureInfo.InvariantCulture) : n.PubDateRaw,
                FormatRelativeDate(n.PublicadoEmUtc),
                n.FonteNome,
                n.Categoria))
            .ToList();
    }

    private static List<Domain.Entities.Noticia> InterleaveBySource(
        Dictionary<string, List<Domain.Entities.Noticia>> porFonte,
        int take)
    {
        if (porFonte.Count == 0)
        {
            return new List<Domain.Entities.Noticia>();
        }

        var resultado = new List<Domain.Entities.Noticia>();
        var fontes = porFonte.Keys.ToList();
        var indices = fontes.ToDictionary(f => f, _ => 0);

        // Round-robin: pegar 1 notícia de cada fonte alternadamente
        while (resultado.Count < take)
        {
            var adicionouNessaRodada = false;

            foreach (var fonte in fontes)
            {
                if (resultado.Count >= take)
                {
                    break;
                }

                var lista = porFonte[fonte];
                var indice = indices[fonte];

                if (indice < lista.Count)
                {
                    resultado.Add(lista[indice]);
                    indices[fonte]++;
                    adicionouNessaRodada = true;
                }
            }

            // Se nenhuma fonte tinha mais itens, encerrar
            if (!adicionouNessaRodada)
            {
                break;
            }
        }

        return resultado;
    }

    private static string TrimToNextPunctuation(string text, int minLength)
    {
        var trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length <= minLength)
        {
            return trimmed;
        }

        var punctuations = new[] { '.', '!', '?', ';', ':' };
        var index = trimmed.IndexOfAny(punctuations, minLength);
        if (index < 0)
        {
            return trimmed[..minLength].Trim();
        }

        return trimmed[..(index + 1)].Trim();
    }

    private static string FormatRelativeDate(DateTime publishedUtc)
    {
        var local = publishedUtc.Kind == DateTimeKind.Utc
            ? publishedUtc.ToLocalTime()
            : DateTime.SpecifyKind(publishedUtc, DateTimeKind.Utc).ToLocalTime();

        var now = DateTime.Now;
        var diff = now - local;
        var diffHours = (int)Math.Floor(diff.TotalHours);
        var diffDays = (int)Math.Floor(diff.TotalDays);

        if (diffHours < 1) return "Agora";
        if (diffHours < 24) return $"{diffHours}h atras";
        if (diffDays == 1) return "Ontem";
        if (diffDays < 7) return $"{diffDays} dias atras";

        return local.ToString("dd MMM", new CultureInfo("pt-BR"));
    }
}
