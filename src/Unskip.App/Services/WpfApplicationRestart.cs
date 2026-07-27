using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Unskip.App.Services;

internal sealed class WpfApplicationRestart : IApplicationRestart
{
    public bool TryRestart()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath)
            || !Path.IsPathFullyQualified(executablePath)
            || !File.Exists(executablePath))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo(executablePath)
            {
                UseShellExecute = false,
            });
            Application.Current.Shutdown();
            return true;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
                or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
