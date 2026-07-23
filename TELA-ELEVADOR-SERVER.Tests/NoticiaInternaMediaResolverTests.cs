using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using TELA_ELEVADOR_SERVER.Api.Controllers;

namespace TELA_ELEVADOR_SERVER.Tests;

/// <summary>
/// Cobre a resolução de tipo de mídia do upload de notícia interna. O bug real
/// era o servidor rejeitar (400) uploads legítimos vindos de celular porque
/// confiava só no Content-Type do navegador — que no mobile costuma vir como
/// "image/jpg", "application/octet-stream" ou com extensão em maiúsculas.
/// </summary>
public class NoticiaInternaMediaResolverTests
{
    private static IFormFile FakeFile(string fileName, string contentType)
    {
        var mock = new Mock<IFormFile>();
        mock.SetupGet(f => f.FileName).Returns(fileName);
        mock.SetupGet(f => f.ContentType).Returns(contentType);
        mock.SetupGet(f => f.Length).Returns(1024);
        return mock.Object;
    }

    [Theory]
    [InlineData("foto.jpg", "image/jpeg", "imagem", ".jpg", "image/jpeg")]
    [InlineData("foto.png", "image/png", "imagem", ".png", "image/png")]
    [InlineData("anim.gif", "image/gif", "imagem", ".gif", "image/gif")]
    [InlineData("foto.webp", "image/webp", "imagem", ".webp", "image/webp")]
    [InlineData("clipe.mp4", "video/mp4", "video", ".mp4", "video/mp4")]
    [InlineData("clipe.webm", "video/webm", "video", ".webm", "video/webm")]
    public void ResolveMedia_TiposPadrao_DeveResolver(
        string fileName, string contentType, string tipo, string ext, string ctEsperado)
    {
        var media = AdminNoticiaInternaController.ResolveMedia(FakeFile(fileName, contentType));

        media.Should().NotBeNull();
        media!.Value.TipoMidia.Should().Be(tipo);
        media.Value.Extensao.Should().Be(ext);
        media.Value.ContentType.Should().Be(ctEsperado);
    }

    [Theory]
    // Android costuma mandar image/jpg (não-padrão) para JPEG.
    [InlineData("foto.jpg", "image/jpg")]
    [InlineData("foto.jpg", "image/pjpeg")]
    public void ResolveMedia_VariantesJpegDeMobile_DeveAceitarComoJpeg(string fileName, string contentType)
    {
        var media = AdminNoticiaInternaController.ResolveMedia(FakeFile(fileName, contentType));

        media.Should().NotBeNull();
        media!.Value.TipoMidia.Should().Be("imagem");
        media.Value.Extensao.Should().Be(".jpg");
        media.Value.ContentType.Should().Be("image/jpeg");
    }

    [Theory]
    // Content-Type genérico/ausente: resolve pela extensão do arquivo.
    [InlineData("application/octet-stream")]
    [InlineData("")]
    public void ResolveMedia_ContentTypeGenerico_DeveResolverPelaExtensao(string contentType)
    {
        var media = AdminNoticiaInternaController.ResolveMedia(FakeFile("IMG_0001.png", contentType));

        media.Should().NotBeNull();
        media!.Value.TipoMidia.Should().Be("imagem");
        media.Value.Extensao.Should().Be(".png");
        media.Value.ContentType.Should().Be("image/png");
    }

    [Fact]
    public void ResolveMedia_ExtensaoMaiuscula_DeveNormalizarParaMinuscula()
    {
        // Content-Type genérico + extensão .JPG (câmera do iPhone/algumas galerias).
        var media = AdminNoticiaInternaController.ResolveMedia(FakeFile("FOTO.JPG", "application/octet-stream"));

        media.Should().NotBeNull();
        media!.Value.Extensao.Should().Be(".jpg");
        media.Value.ContentType.Should().Be("image/jpeg");
    }

    [Theory]
    [InlineData("documento.pdf", "application/pdf")]
    [InlineData("foto.heic", "image/heic")]
    [InlineData("arquivo", "application/octet-stream")]
    public void ResolveMedia_FormatoNaoSuportado_DeveRetornarNull(string fileName, string contentType)
    {
        var media = AdminNoticiaInternaController.ResolveMedia(FakeFile(fileName, contentType));

        media.Should().BeNull();
    }
}
