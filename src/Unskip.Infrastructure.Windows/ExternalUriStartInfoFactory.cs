using System.Diagnostics;

namespace Unskip.Infrastructure.Windows;

internal static class ExternalUriStartInfoFactory
{
    public static ProcessStartInfo Create(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (!uri.IsAbsoluteUri
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException(
                "External links must use an absolute HTTPS URI without user information.",
                nameof(uri));
        }

        return new ProcessStartInfo
        {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true,
            ErrorDialog = false,
        };
    }
}
