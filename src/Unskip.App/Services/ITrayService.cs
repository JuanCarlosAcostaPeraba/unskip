namespace Unskip.App.Services;

public interface ITrayService : IDisposable
{
    event EventHandler? OpenMainRequested;

    event EventHandler? QuickSendRequested;

    event EventHandler? ExitRequested;
}
