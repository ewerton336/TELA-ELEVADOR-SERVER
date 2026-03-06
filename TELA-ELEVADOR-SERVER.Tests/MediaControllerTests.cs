using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TELA_ELEVADOR_SERVER.Api.Controllers;

namespace TELA_ELEVADOR_SERVER.Tests;

public class MediaControllerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MediaController _controller;
    private readonly List<IDisposable> _disposables = new();

    public MediaControllerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"media_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MediaStorage:BasePath"] = _tempDir
            })
            .Build();

        _controller = new MediaController(config);
    }

    public void Dispose()
    {
        foreach (var d in _disposables)
            d.Dispose();
        _disposables.Clear();

        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private T TrackDisposable<T>(T result) where T : IActionResult
    {
        if (result is FileStreamResult fsr)
            _disposables.Add(fsr.FileStream);
        return result;
    }

    // ── Path traversal protection ──

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("..\\windows\\system32")]
    [InlineData("subdir/file.jpg")]
    [InlineData("subdir\\file.jpg")]
    public void GetMedia_PathTraversal_ShouldReturnBadRequest(string fileName)
    {
        var result = _controller.GetMedia(fileName);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetMedia_EmptyFileName_ShouldReturnBadRequest(string fileName)
    {
        var result = _controller.GetMedia(fileName);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── File not found ──

    [Fact]
    public void GetMedia_NonExistentFile_ShouldReturnNotFound()
    {
        var result = _controller.GetMedia("non-existent-file.jpg");
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Successful serve ──

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("image.png", "image/png")]
    [InlineData("anim.gif", "image/gif")]
    [InlineData("modern.webp", "image/webp")]
    [InlineData("clip.mp4", "video/mp4")]
    [InlineData("clip.webm", "video/webm")]
    public void GetMedia_ExistingFile_ShouldReturnFileWithCorrectContentType(string fileName, string expectedContentType)
    {
        File.WriteAllBytes(Path.Combine(_tempDir, fileName), [0x00, 0x01, 0x02]);

        var result = TrackDisposable(_controller.GetMedia(fileName));

        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be(expectedContentType);
        fileResult.EnableRangeProcessing.Should().BeTrue();
    }

    [Fact]
    public void GetMedia_UnknownExtension_ShouldReturnOctetStream()
    {
        File.WriteAllBytes(Path.Combine(_tempDir, "data.bin"), [0x00]);

        var result = TrackDisposable(_controller.GetMedia("data.bin"));

        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be("application/octet-stream");
    }

    // ── GUID-based filename (real scenario) ──

    [Fact]
    public void GetMedia_GuidFilename_ShouldServeCorrectly()
    {
        var guidName = $"{Guid.NewGuid()}.mp4";
        File.WriteAllBytes(Path.Combine(_tempDir, guidName), [0xFF]);

        var result = TrackDisposable(_controller.GetMedia(guidName));

        result.Should().BeOfType<FileStreamResult>();
    }
}
