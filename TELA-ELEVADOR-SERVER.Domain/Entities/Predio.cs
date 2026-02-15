namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class Predio : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string OrientationMode { get; set; } = "auto";

    public ICollection<Sindico> Sindicos { get; set; } = new List<Sindico>();
    public ICollection<Aviso> Avisos { get; set; } = new List<Aviso>();
    public ICollection<PreferenciaNoticia> PreferenciasNoticia { get; set; } = new List<PreferenciaNoticia>();
}
