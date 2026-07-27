using Unskip.App.Services;

namespace Unskip.App.Tests;

public sealed class ResidentApplicationControllerTests
{
    [Fact]
    public void ClosingAWindowHidesItWhileApplicationIsResident()
    {
        var context = CreateContext();

        var intercepted = context.Controller.TryHideOnClose(context.MainWindow);

        Assert.True(intercepted);
        Assert.Equal(1, context.MainWindow.HideCount);
        Assert.Equal(0, context.Shutdown.RequestCount);
    }

    [Fact]
    public void RequestedShutdownIsNeverIntercepted()
    {
        var context = CreateContext();
        context.ExitState.RequestExit();

        var intercepted = context.Controller.TryHideOnClose(context.MainWindow);

        Assert.False(intercepted);
        Assert.Equal(0, context.MainWindow.HideCount);
    }

    [Fact]
    public void TrayOpenActivatesTheExistingMainWindow()
    {
        var context = CreateContext();

        context.Tray.RaiseOpenMain();
        context.Tray.RaiseOpenMain();

        Assert.Equal(2, context.MainWindow.ShowCount);
    }

    [Fact]
    public async Task QuickSendRefreshesBeforeActivatingTheExistingPanel()
    {
        var refreshCount = 0;
        var context = CreateContext(() =>
        {
            refreshCount++;
            return Task.CompletedTask;
        });

        await context.Controller.ShowQuickSendAsync();
        await context.Controller.ShowQuickSendAsync();

        Assert.Equal(2, refreshCount);
        Assert.Equal(2, context.QuickWindow.ShowCount);
    }

    [Fact]
    public void ExitDisposesTrayAndRequestsRealShutdownOnce()
    {
        var context = CreateContext();

        context.Tray.RaiseExit();
        context.Controller.Exit();

        Assert.True(context.ExitState.IsExitRequested);
        Assert.Equal(1, context.Tray.DisposeCount);
        Assert.Equal(1, context.Shutdown.RequestCount);
    }

    private static TestContext CreateContext(Func<Task>? refresh = null)
    {
        var main = new StubResidentWindow();
        var quick = new StubResidentWindow();
        var tray = new StubTrayService();
        var shutdown = new StubShutdown();
        var exitState = new ApplicationExitState();
        var controller = new ResidentApplicationController(
            main,
            quick,
            tray,
            shutdown,
            exitState,
            refresh ?? (() => Task.CompletedTask));
        return new TestContext(controller, main, quick, tray, shutdown, exitState);
    }

    private sealed record TestContext(
        ResidentApplicationController Controller,
        StubResidentWindow MainWindow,
        StubResidentWindow QuickWindow,
        StubTrayService Tray,
        StubShutdown Shutdown,
        ApplicationExitState ExitState);

    private sealed class StubResidentWindow : IResidentWindow
    {
        public int ShowCount { get; private set; }

        public int HideCount { get; private set; }

        public void ShowAndActivate()
        {
            ShowCount++;
        }

        public void Hide()
        {
            HideCount++;
        }
    }

    private sealed class StubTrayService : ITrayService
    {
        public event EventHandler? OpenMainRequested;

        public event EventHandler? QuickSendRequested;

        public event EventHandler? ExitRequested;

        public int DisposeCount { get; private set; }

        public void RaiseOpenMain()
        {
            OpenMainRequested?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseExit()
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseQuickSend()
        {
            QuickSendRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private sealed class StubShutdown : IApplicationShutdown
    {
        public int RequestCount { get; private set; }

        public void Shutdown()
        {
            RequestCount++;
        }
    }
}
