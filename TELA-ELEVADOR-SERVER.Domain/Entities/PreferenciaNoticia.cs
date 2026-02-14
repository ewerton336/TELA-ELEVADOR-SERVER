namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class PreferenciaNoticia
{
    public int PredioId { get; set; }
    public int FonteNoticiaId { get; set; }
    public bool Habilitado { get; set; }
    public DateTime CriadoEm { get; set; }

    public Predio? Predio { get; set; }
    public FonteNoticia? FonteNoticia { get; set; }
}
