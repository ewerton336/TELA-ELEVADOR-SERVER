using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TELA_ELEVADOR_SERVER.Api.Controllers;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Tests;

public class PublicPrediosControllerTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"predios_test_{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetPredios_DeveRetornarListaOrdenadaPorNomeComCidade()
    {
        using var context = CreateContext();

        var santos = new Cidade { Id = 1, Nome = "santos", NomeExibicao = "Santos, SP" };
        context.Cidades.Add(santos);
        context.Predios.AddRange(
            new Predio { Id = 10, Slug = "beta", Nome = "Edifício Beta", CidadeId = 1 },
            new Predio { Id = 20, Slug = "alfa", Nome = "Edifício Alfa", CidadeId = 1 },
            new Predio { Id = 30, Slug = "gama", Nome = "Edifício Gama", CidadeId = null });
        await context.SaveChangesAsync();

        var controller = new PublicPrediosController(context);

        var result = await controller.GetPredios();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new[]
        {
            new { Slug = "alfa", Nome = "Edifício Alfa", cidade = "Santos, SP" },
            new { Slug = "beta", Nome = "Edifício Beta", cidade = "Santos, SP" },
            new { Slug = "gama", Nome = "Edifício Gama", cidade = "Sem cidade" },
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetPredios_SemPredios_DeveRetornarListaVazia()
    {
        using var context = CreateContext();
        var controller = new PublicPrediosController(context);

        var result = await controller.GetPredios();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<System.Collections.IEnumerable>()
            .Which.Cast<object>().Should().BeEmpty();
    }

    [Fact]
    public void Controller_DeveSerPublico_SemAuthorize()
    {
        var type = typeof(PublicPrediosController);

        type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Should().BeEmpty();

        type.GetMethod(nameof(PublicPrediosController.GetPredios))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Should().BeEmpty();
    }
}
