namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class ClimaAtual : BaseEntity
{
    public int CidadeId { get; set; }
    public int Temperatura { get; set; }
    public int SensacaoTermica { get; set; }
    public int Umidade { get; set; }
    public double VentoVelocidade { get; set; }
    public int CodigoWmo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
    public bool IsDay { get; set; }
    public DateTime AtualizadoEm { get; set; }

    public Cidade? Cidade { get; set; }
}
