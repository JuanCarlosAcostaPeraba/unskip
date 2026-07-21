using System.Text;

namespace Unskip.Infrastructure.Windows;

internal static class DiagnosticSanitizer
{
    private const int MaximumDiagnosticLength = 2_048;
    private const string MessageReplacement = "[message omitted]";

    public static string Sanitize(string? diagnostic, string messageBody)
    {
        if (string.IsNullOrWhiteSpace(diagnostic))
        {
            return string.Empty;
        }

        var withoutMessage = string.IsNullOrEmpty(messageBody)
            ? diagnostic
            : diagnostic.Replace(messageBody, MessageReplacement, StringComparison.Ordinal);

        var sanitized = new StringBuilder(Math.Min(withoutMessage.Length, MaximumDiagnosticLength));
        foreach (var character in withoutMessage)
        {
            if (sanitized.Length == MaximumDiagnosticLength)
            {
                break;
            }

            sanitized.Append(char.IsControl(character) && character is not '\r' and not '\n' and not '\t'
                ? ' '
                : character);
        }

        return sanitized.ToString().Trim();
    }
}
