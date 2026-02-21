namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class Cidade : BaseEntity
{
    /// <summary>
    /// Nome normalizado (lowercase, sem acentos) - usado para busca
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Nome para exibição (ex: "Marília, SP")
    /// </summary>
    public string NomeExibicao { get; set; } = string.Empty;

    /// <summary>
    /// Latitude para previsão do tempo
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude para previsão do tempo
    /// </summary>
    public double Longitude { get; set; }

    public ICollection<Predio> Predios { get; set; } = new List<Predio>();
    public ICollection<ClimaPrevisao> ClimaPrevisoesData { get; set; } = new List<ClimaPrevisao>();
}
