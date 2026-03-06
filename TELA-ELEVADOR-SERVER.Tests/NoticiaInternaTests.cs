using FluentAssertions;
using TELA_ELEVADOR_SERVER.Domain.Entities;

namespace TELA_ELEVADOR_SERVER.Tests;

public class NoticiaInternaTests
{
    [Fact]
    public void NovaNoticiaInterna_DeveInicializarComDefaults()
    {
        var noticia = new NoticiaInterna();

        noticia.TipoMidia.Should().Be("imagem");
        noticia.Ativo.Should().BeTrue();
        noticia.Titulo.Should().BeEmpty();
        noticia.NomeArquivo.Should().BeEmpty();
        noticia.NomeArquivoOriginal.Should().BeEmpty();
        noticia.ContentType.Should().BeEmpty();
        noticia.Subtitulo.Should().BeNull();
        noticia.InicioEm.Should().BeNull();
        noticia.FimEm.Should().BeNull();
    }

    [Fact]
    public void NoticiaInterna_DeveAtribuirPropriedadesCorretamente()
    {
        var agora = DateTime.UtcNow;
        var inicio = agora.AddDays(-1);
        var fim = agora.AddDays(7);

        var noticia = new NoticiaInterna
        {
            Id = 42,
            PredioId = 1,
            Titulo = "Festa no Salão",
            Subtitulo = "Dia 15 às 19h",
            TipoMidia = "video",
            NomeArquivo = "abc123.mp4",
            NomeArquivoOriginal = "convite.mp4",
            ContentType = "video/mp4",
            InicioEm = inicio,
            FimEm = fim,
            Ativo = false,
            CriadoEm = agora,
        };

        noticia.Id.Should().Be(42);
        noticia.PredioId.Should().Be(1);
        noticia.Titulo.Should().Be("Festa no Salão");
        noticia.Subtitulo.Should().Be("Dia 15 às 19h");
        noticia.TipoMidia.Should().Be("video");
        noticia.NomeArquivo.Should().Be("abc123.mp4");
        noticia.NomeArquivoOriginal.Should().Be("convite.mp4");
        noticia.ContentType.Should().Be("video/mp4");
        noticia.InicioEm.Should().Be(inicio);
        noticia.FimEm.Should().Be(fim);
        noticia.Ativo.Should().BeFalse();
        noticia.CriadoEm.Should().Be(agora);
    }

    [Fact]
    public void NoticiaInterna_HerdaBaseEntity()
    {
        var noticia = new NoticiaInterna();
        noticia.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void NoticiaInterna_NavigationPropertyPredioPodeSerNull()
    {
        var noticia = new NoticiaInterna();
        noticia.Predio.Should().BeNull();
    }

    [Fact]
    public void NoticiaInterna_NavigationPropertyPredioAtribuivel()
    {
        var predio = new Predio { Id = 5, Slug = "gramado", Nome = "Gramado Central" };
        var noticia = new NoticiaInterna { PredioId = 5, Predio = predio };

        noticia.Predio.Should().BeSameAs(predio);
        noticia.Predio!.Slug.Should().Be("gramado");
    }
}
