using System.Windows;

namespace Unskip.App.Services;

public sealed class WpfApplicationShutdown : IApplicationShutdown
{
    public void Shutdown()
    {
        Application.Current.Shutdown();
    }
}
