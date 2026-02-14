using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;
using TELA_ELEVADOR_SERVER.Infrastructure.Security;

namespace TELA_ELEVADOR_SERVER.Api.Controllers;

[ApiController]
[Route("api/{slug}/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext dbContext, IPasswordHasher passwordHasher, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromRoute] string slug, [FromBody] LoginRequest request)
    {
        var predio = await _dbContext.Predios
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);

        Sindico? sindico = null;

        // Try to find a syndico for this building
        if (predio is not null)
        {
            sindico = await _dbContext.Sindicos
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.PredioId == predio.Id && s.Usuario == request.Usuario);
        }

        // If not found, try to find a developer user (they can access any building)
        if (sindico is null)
        {
            sindico = await _dbContext.Sindicos
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Usuario == request.Usuario && s.Role == "Developer");
        }

        if (predio is null && sindico is null)
        {
            return NotFound(new { message = "Predio nao encontrado." });
        }

        if (sindico is null)
        {
            return Unauthorized(new { message = "Credenciais invalidas." });
        }

        if (!_passwordHasher.VerifyPassword(request.Senha, sindico.SenhaHash, sindico.SenhaSalt))
        {
            return Unauthorized(new { message = "Credenciais invalidas." });
        }

        // If it's a developer and a specific building was accessed, use that building
        // Otherwise use master for developer or the building's ID for syndicos
        var tokenPredioId = predio?.Id ?? 0;
        var tokenSlug = predio?.Slug ?? "master";
        var token = BuildToken(tokenPredioId, tokenSlug, sindico.Usuario, sindico.Role);
        return Ok(new LoginResponse(token));
    }

    [Authorize(Policy = "PredioMatchesSlug")]
    [HttpGet("me")]
    public IActionResult Me([FromRoute] string slug)
    {
        var usuario = User.FindFirst("usuario")?.Value;
        return Ok(new { slug, usuario });
    }

    private string BuildToken(int predioId, string slug, string usuario, string role)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection.GetValue<string>("Key") ?? string.Empty;
        var issuer = jwtSection.GetValue<string>("Issuer");
        var audience = jwtSection.GetValue<string>("Audience");

        var claims = new List<Claim>
        {
            new("predioId", predioId.ToString()),
            new("slug", slug),
            new("usuario", usuario),
            new("role", role),
            new(ClaimTypes.Name, usuario)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public sealed record LoginRequest(string Usuario, string Senha);
    public sealed record LoginResponse(string Token);
}
