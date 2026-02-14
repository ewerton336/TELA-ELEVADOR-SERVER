namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class PreferenciaNoticia
{
    public Guid PredioId { get; set; }
    public Guid FonteNoticiaId { get; set; }
    public bool Habilitado { get; set; }
    public DateTime CriadoEm { get; set; }

    public Predio? Predio { get; set; }
    public FonteNoticia? FonteNoticia { get; set; }
}
