namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class Predio : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// ID da cidade (novo modelo - substitui propriedade anterior de string Cidade)
    /// </summary>
    public int? CidadeId { get; set; }

    public string OrientationMode { get; set; } = "auto";

    // Módulos configuráveis da tela do elevador
    public bool ModuloBuildingNotice { get; set; } = true;
    public bool ModuloWeather { get; set; } = true;
    public bool ModuloHeadlineNews { get; set; } = true;
    public bool ModuloNewsTicker { get; set; } = true;

    public ICollection<Sindico> Sindicos { get; set; } = new List<Sindico>();
    public ICollection<Aviso> Avisos { get; set; } = new List<Aviso>();
    public ICollection<NoticiaInterna> NoticiasInternas { get; set; } = new List<NoticiaInterna>();
    public ICollection<PreferenciaNoticia> PreferenciasNoticia { get; set; } = new List<PreferenciaNoticia>();

    public Cidade? Cidade { get; set; }
}
