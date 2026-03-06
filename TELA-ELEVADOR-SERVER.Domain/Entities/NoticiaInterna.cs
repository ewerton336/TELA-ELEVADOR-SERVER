namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class NoticiaInterna : BaseEntity
{
    public int PredioId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public string TipoMidia { get; set; } = "imagem"; // "imagem" | "video"
    public string NomeArquivo { get; set; } = string.Empty;
    public string NomeArquivoOriginal { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime? InicioEm { get; set; }
    public DateTime? FimEm { get; set; }
    public bool Ativo { get; set; } = true;

    public Predio? Predio { get; set; }
}
