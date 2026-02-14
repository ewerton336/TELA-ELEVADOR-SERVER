namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public sealed class Sindico : BaseEntity
{
    public Guid PredioId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string SenhaSalt { get; set; } = string.Empty;

    public Predio? Predio { get; set; }
}
