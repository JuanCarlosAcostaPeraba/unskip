using System.Diagnostics;

namespace Unskip.Infrastructure.Windows;

internal static class UpdateInstallerStartInfoFactory
{
    public static ProcessStartInfo Create(string installerPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installerPath);

        var fullPath = Path.GetFullPath(installerPath);
        if (!string.Equals(Path.GetExtension(fullPath), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The update installer must be an executable file.", nameof(installerPath));
        }

        return new ProcessStartInfo
        {
            FileName = fullPath,
            WorkingDirectory = Path.GetDirectoryName(fullPath),
            UseShellExecute = false,
            CreateNoWindow = false,
            ErrorDialog = false,
        };
    }
}
