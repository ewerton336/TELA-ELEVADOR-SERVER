using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TELA_ELEVADOR_SERVER.Domain.Entities;
using TELA_ELEVADOR_SERVER.EntityFrameworkCore.Persistence;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Seeding;

public sealed class CidadeSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CidadeSeeder> _logger;
    private readonly HttpClient _httpClient;
    private readonly IHostEnvironment _environment;

    private const string IbgeBaseUrl = "https://servicodados.ibge.gov.br/api/v1/localidades/estados";
    private const string SaoPauloSigla = "SP";
    private const string MunicipiosJsonPath = "Data/municipios.json"; // Caminho relativo à pasta Api

    public CidadeSeeder(AppDbContext dbContext, ILogger<CidadeSeeder> logger, HttpClient httpClient, IHostEnvironment environment)
    {
        _dbContext = dbContext;
        _logger = logger;
        _httpClient = httpClient;
        _environment = environment;
    }

    /// <summary>
    /// Popula a tabela Cidade com todas as cidades de São Paulo (IBGE)
    /// Esta operação deve ser executada UMA ÚNICA VEZ (ou quando tabela está vazia)
    /// Em Development, limpamos a tabela antes de popular
    /// </summary>
    public async Task SeedCidadesSaoPauloAsync()
    {
        try
        {
            _logger.LogInformation("=== [CidadeSeeder] Iniciando verificação de cidades (Environment: {Environment}) ===",
                _environment.EnvironmentName);

            // Verificar se já existe cidades populadas
            var cidadesExistentes = await _dbContext.Cidades.CountAsync();

            _logger.LogInformation("[CidadeSeeder] Cidades existentes: {Count}", cidadesExistentes);

            if (cidadesExistentes > 0 && !_environment.IsDevelopment())
            {
                _logger.LogInformation("✓ [CidadeSeeder] Tabela Cidade já populada com {CidadeCount} registros. Seeder ignorado (Production).", cidadesExistentes);
                return;
            }

            if (cidadesExistentes > 0 && _environment.IsDevelopment())
            {
                _logger.LogInformation("[CidadeSeeder] Limpando {Count} cidades em Development para repovoar...", cidadesExistentes);
                await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Cidade\" RESTART IDENTITY CASCADE");
                _logger.LogInformation("✓ [CidadeSeeder] Tabela Cidade limpa.");
            }

            _logger.LogInformation("[CidadeSeeder] Iniciando consumo da API IBGE para São Paulo...");

            var cidades = await FetchCidadesSaoPauloFromIbgeAsync();

            if (cidades == null || cidades.Count == 0)
            {
                _logger.LogWarning("⚠️ [CidadeSeeder] Nenhuma cidade foi retornada da API IBGE. Seed abortado.");
                return;
            }

            _logger.LogInformation("[CidadeSeeder] ✓ Obtidas {CidadeCount} cidades de São Paulo da IBGE. Salvando no banco...", cidades.Count);

            // Verificar quais cidades já existem para evitar duplicatas
            var nomesExistentes = await _dbContext.Cidades
                .Select(c => c.Nome)
                .ToListAsync();

            _logger.LogInformation("[CidadeSeeder] Nomes existentes no banco: {Count}", nomesExistentes.Count);

            var cidadesParaInserir = cidades
                .Where(c => !nomesExistentes.Contains(c.Nome))
                .ToList();

            _logger.LogInformation("[CidadeSeeder] Cidades para inserir após filtro: {Count}", cidadesParaInserir.Count);

            if (cidadesParaInserir.Count == 0)
            {
                _logger.LogInformation("✓ [CidadeSeeder] Todas as {CidadeCount} cidades já existem no banco. Seed abortado.", cidades.Count);
                return;
            }

            _logger.LogInformation("[CidadeSeeder] Inserindo {NovasCidades} novas cidades (de {Total} retornadas)...", cidadesParaInserir.Count, cidades.Count);

            try
            {
                _logger.LogInformation("[CidadeSeeder] AddRangeAsync iniciado...");
                await _dbContext.Cidades.AddRangeAsync(cidadesParaInserir);
                _logger.LogInformation("[CidadeSeeder] AddRangeAsync completado. Chamando SaveChangesAsync...");

                var changeCount = await _dbContext.SaveChangesAsync();
                _logger.LogInformation("[CidadeSeeder] SaveChangesAsync completado. {ChangeCount} registros salvos.", changeCount);
            }
            catch (Exception savEx)
            {
                _logger.LogError(savEx, "[CidadeSeeder] Erro durante AddRange/SaveChanges: {Message}", savEx.Message);
                throw;
            }

            _logger.LogInformation("✓✓✓ [CidadeSeeder] SEED DE CIDADES CONCLUÍDO: {CidadeCount} cidades de SP populadas com sucesso!", cidadesParaInserir.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [CidadeSeeder] Erro CRÍTICO ao fazer seed de cidades via IBGE: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Busca todas as cidades de São Paulo do arquivo JSON local
    /// </summary>
    private async Task<List<Cidade>> FetchCidadesSaoPauloFromIbgeAsync()
    {
        try
        {
            // Procurar o arquivo municipios.json na pasta da aplicação
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, MunicipiosJsonPath),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "TELA-ELEVADOR-SERVER.Api", MunicipiosJsonPath),
                Path.Combine(Directory.GetCurrentDirectory(), MunicipiosJsonPath),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "TELA-ELEVADOR-SERVER.Api", MunicipiosJsonPath)
            };

            string? jsonPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    jsonPath = path;
                    _logger.LogInformation("[CidadeSeeder] ✓ Arquivo encontrado em: {Path}", jsonPath);
                    break;
                }
            }

            if (string.IsNullOrEmpty(jsonPath))
            {
                _logger.LogError("[CidadeSeeder] ❌ Arquivo municipios.json não encontrado!");
                return new List<Cidade>();
            }

            // Ler o arquivo JSON
            _logger.LogInformation("[CidadeSeeder] Lendo arquivo: {Path}", jsonPath);
            var json = await File.ReadAllTextAsync(jsonPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var municipios = JsonSerializer.Deserialize<List<IbgeMunicipio>>(json, options);

            if (municipios == null || municipios.Count == 0)
            {
                _logger.LogWarning("[CidadeSeeder] Arquivo JSON está vazio!");
                return new List<Cidade>();
            }

            _logger.LogInformation("[CidadeSeeder] Total de municipios carregados: {Count}", municipios.Count);

            var cidades = new List<Cidade>();
            var nomesAdicionados = new HashSet<string>();
            var duplicatasCount = 0;

            foreach (var municipio in municipios)
            {
                if (string.IsNullOrEmpty(municipio.Nome))
                {
                    _logger.LogWarning("[CidadeSeeder] Municipio com nome vazio encontrado!");
                    continue;
                }

                var nomeNormalizado = NormalizeString(municipio.Nome);

                if (nomesAdicionados.Contains(nomeNormalizado))
                {
                    duplicatasCount++;
                    if (duplicatasCount <= 3)
                    {
                        _logger.LogWarning("[CidadeSeeder] Duplicado: {Original} -> {Normalizado}", municipio.Nome, nomeNormalizado);
                    }
                    continue;
                }

                nomesAdicionados.Add(nomeNormalizado);

                var cidade = new Cidade
                {
                    Nome = nomeNormalizado,
                    NomeExibicao = $"{municipio.Nome}, SP",
                    Latitude = 0,
                    Longitude = 0,
                    CriadoEm = DateTime.UtcNow
                };

                cidades.Add(cidade);
            }

            _logger.LogInformation("[CidadeSeeder] Processados: {Count} municipios únicos", cidades.Count);

            return cidades;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "[CidadeSeeder] Arquivo não encontrado");
            return new List<Cidade>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[CidadeSeeder] Erro ao fazer parse do JSON");
            return new List<Cidade>();
        }
    }

    /// <summary>
    /// Normaliza string para lowercase sem acentos (mesmo padrão do CidadeService)
    /// </summary>
    private static string NormalizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var normalized = input
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD);

        var chars = new List<char>();
        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                chars.Add(c);
            }
        }

        return new string(chars.ToArray());
    }

    // DTOs para desserialização IBGE
    private class IbgeMunicipio
    {
        /// <summary>
        /// Nome do município
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Geometria com coordenadas
        /// </summary>
        public IbgeGeometria? Geometria { get; set; }
    }

    private class IbgeGeometria
    {
        /// <summary>
        /// Coordenadas do centroide [longitude, latitude]
        /// </summary>
        public double[]? Centroide { get; set; }
    }
}
