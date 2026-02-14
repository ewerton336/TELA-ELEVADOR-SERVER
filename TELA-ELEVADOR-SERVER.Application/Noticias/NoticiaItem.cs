namespace TELA_ELEVADOR_SERVER.Application.Noticias;

public sealed record NoticiaItem(
    string Id,
    string Title,
    string Description,
    string Link,
    string Thumbnail,
    string PubDate,
    string PubDateFormatted,
    string Source,
    string? Category);
