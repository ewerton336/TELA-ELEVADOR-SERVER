namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class FonteNoticia : BaseEntity
{
    public string Chave { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string UrlBase { get; set; } = string.Empty;
    public bool Ativo { get; set; }

    public ICollection<PreferenciaNoticia> PreferenciasNoticia { get; set; } = new List<PreferenciaNoticia>();
}
