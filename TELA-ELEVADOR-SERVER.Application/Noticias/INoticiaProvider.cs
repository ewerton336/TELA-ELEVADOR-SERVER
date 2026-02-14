namespace TELA_ELEVADOR_SERVER.Application.Noticias;

public interface INoticiaProvider
{
    string Chave { get; }
    Task<List<NoticiaItem>> BuscarUltimasAsync();
}
