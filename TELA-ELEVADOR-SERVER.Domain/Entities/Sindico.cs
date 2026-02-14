namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class Sindico : BaseEntity
{
    public int? PredioId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string SenhaSalt { get; set; } = string.Empty;
    public string Role { get; set; } = "Sindico";

    public Predio? Predio { get; set; }
}
