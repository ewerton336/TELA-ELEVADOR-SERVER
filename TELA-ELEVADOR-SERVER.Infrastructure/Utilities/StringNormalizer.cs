using System.Globalization;
using System.Text;

namespace TELA_ELEVADOR_SERVER.Infrastructure.Utilities;

public static class StringNormalizer
{
    /// <summary>
    /// Normaliza string removendo acentos e convertendo para lowercase
    /// Exemplo: "Marília" → "marilia"
    /// </summary>
    public static string NormalizeForSearch(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remover acentos
        var nfdForm = input.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in nfdForm)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Retornar em lowercase
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLower();
    }
}
