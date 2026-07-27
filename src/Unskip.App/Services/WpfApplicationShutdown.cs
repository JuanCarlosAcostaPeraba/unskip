using System.Windows;

namespace Unskip.App.Services;

internal sealed class WpfApplicationShutdown(ApplicationExitState exitState) : IApplicationShutdown
{
    private readonly ApplicationExitState _exitState =
        exitState ?? throw new ArgumentNullException(nameof(exitState));

    public void Shutdown()
    {
        _exitState.RequestExit();
        Application.Current.Shutdown();
    }
}
