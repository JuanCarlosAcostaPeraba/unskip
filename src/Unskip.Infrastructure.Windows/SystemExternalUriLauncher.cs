using System.ComponentModel;
using System.Diagnostics;
using Unskip.Core.Links;

namespace Unskip.Infrastructure.Windows;

public sealed class SystemExternalUriLauncher : IExternalUriLauncher
{
    public bool TryOpen(Uri uri)
    {
        try
        {
            using var process = Process.Start(ExternalUriStartInfoFactory.Create(uri));
            return process is not null;
        }
        catch (ArgumentException)
        {
            return false;
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
