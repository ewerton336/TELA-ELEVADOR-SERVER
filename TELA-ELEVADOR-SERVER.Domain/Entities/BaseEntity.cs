namespace TELA_ELEVADOR_SERVER.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CriadoEm { get; set; }
}
