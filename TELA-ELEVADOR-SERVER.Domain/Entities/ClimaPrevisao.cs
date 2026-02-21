namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class ClimaPrevisao : BaseEntity
{
    public int CidadeId { get; set; }
    public DateOnly Data { get; set; }
    public int TemperaturaMaxima { get; set; }
    public int TemperaturaMinima { get; set; }
    public int CodigoWmo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
    public DateTime AtualizadoEm { get; set; }

    public Cidade? Cidade { get; set; }
}
