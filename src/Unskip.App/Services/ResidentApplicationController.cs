namespace Unskip.App.Services;

public sealed class ResidentApplicationController : IDisposable
{
    private readonly ApplicationExitState _exitState;
    private readonly IResidentWindow _mainWindow;
    private readonly Func<Task> _refreshQuickSend;
    private readonly IResidentWindow _quickSendWindow;
    private readonly IApplicationShutdown _shutdown;
    private readonly ITrayService _tray;
    private bool _isDisposed;

    internal ResidentApplicationController(
        IResidentWindow mainWindow,
        IResidentWindow quickSendWindow,
        ITrayService tray,
        IApplicationShutdown shutdown,
        ApplicationExitState exitState,
        Func<Task> refreshQuickSend)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        _quickSendWindow = quickSendWindow ?? throw new ArgumentNullException(nameof(quickSendWindow));
        _tray = tray ?? throw new ArgumentNullException(nameof(tray));
        _shutdown = shutdown ?? throw new ArgumentNullException(nameof(shutdown));
        _exitState = exitState ?? throw new ArgumentNullException(nameof(exitState));
        _refreshQuickSend = refreshQuickSend ?? throw new ArgumentNullException(nameof(refreshQuickSend));

        _tray.OpenMainRequested += OnOpenMainRequested;
        _tray.QuickSendRequested += OnQuickSendRequested;
        _tray.ExitRequested += OnExitRequested;
    }

    public bool TryHideOnClose(IResidentWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (_exitState.IsExitRequested)
        {
            return false;
        }

        window.Hide();
        return true;
    }

    public void ShowMainWindow()
    {
        _mainWindow.ShowAndActivate();
    }

    public async Task ShowQuickSendAsync()
    {
        await _refreshQuickSend().ConfigureAwait(true);
        _quickSendWindow.ShowAndActivate();
    }

    public void Exit()
    {
        if (_exitState.IsExitRequested)
        {
            return;
        }

        _exitState.RequestExit();
        Dispose();
        _shutdown.Shutdown();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _tray.OpenMainRequested -= OnOpenMainRequested;
        _tray.QuickSendRequested -= OnQuickSendRequested;
        _tray.ExitRequested -= OnExitRequested;
        _tray.Dispose();
    }

    private void OnOpenMainRequested(object? sender, EventArgs eventArgs)
    {
        ShowMainWindow();
    }

    private async void OnQuickSendRequested(object? sender, EventArgs eventArgs)
    {
        await ShowQuickSendAsync().ConfigureAwait(true);
    }

    private void OnExitRequested(object? sender, EventArgs eventArgs)
    {
        Exit();
    }
}
