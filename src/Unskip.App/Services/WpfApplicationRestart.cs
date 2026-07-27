using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Unskip.App.Services;

internal sealed class WpfApplicationRestart(ApplicationExitState exitState) : IApplicationRestart
{
    private readonly ApplicationExitState _exitState =
        exitState ?? throw new ArgumentNullException(nameof(exitState));

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
            _exitState.RequestExit();
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
