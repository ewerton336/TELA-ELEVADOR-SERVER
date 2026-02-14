namespace TELA_ELEVADOR_SERVER.Application.Noticias;

public interface INoticiaService
{
    Task<List<NoticiaItem>> BuscarNoticiasAsync(IEnumerable<string> chaves);
}
