namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class Aviso : BaseEntity
{
    public int PredioId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public DateTime? InicioEm { get; set; }
    public DateTime? FimEm { get; set; }
    public bool Ativo { get; set; }
    public string Prioridade { get; set; } = "normal";

    public Predio? Predio { get; set; }
}
