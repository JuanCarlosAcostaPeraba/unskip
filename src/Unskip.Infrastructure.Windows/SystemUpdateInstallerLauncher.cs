using System.ComponentModel;
using System.Diagnostics;
using Unskip.Core.Updates;

namespace Unskip.Infrastructure.Windows;

public sealed class SystemUpdateInstallerLauncher : IUpdateInstallerLauncher
{
    public bool TryLaunch(string installerPath)
    {
        try
        {
            using var process = Process.Start(UpdateInstallerStartInfoFactory.Create(installerPath));
            return process is not null;
        }
        catch (Win32Exception)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
