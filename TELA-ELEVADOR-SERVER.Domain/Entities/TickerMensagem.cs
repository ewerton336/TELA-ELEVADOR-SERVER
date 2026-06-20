namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class TickerMensagem : BaseEntity
{
    public int PredioId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public int Ordem { get; set; }

    public Predio? Predio { get; set; }
}
