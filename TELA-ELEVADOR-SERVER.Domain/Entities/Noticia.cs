namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class Noticia : BaseEntity
{
    public string FonteChave { get; set; } = string.Empty;
    public string FonteNome { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string ImagemUrl { get; set; } = string.Empty;
    public string PubDateRaw { get; set; } = string.Empty;
    public DateTime PublicadoEmUtc { get; set; }
    public string? Categoria { get; set; }
}
