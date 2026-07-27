using System.Windows;
using Unskip.App.ViewModels;
using Unskip.App.Views;
using Unskip.Core.Messaging;

namespace Unskip.App.Services;

internal sealed class WpfUrgentAttentionPreviewService(
    IVirtualScreenProvider virtualScreenProvider) : IUrgentAttentionPreviewService
{
    private static readonly TimeSpan PreviewTimeout = TimeSpan.FromSeconds(60);
    private readonly IVirtualScreenProvider _virtualScreenProvider =
        virtualScreenProvider ?? throw new ArgumentNullException(nameof(virtualScreenProvider));
    private UrgentAttentionOverlayWindow? _activeWindow;
    private Task? _activeSession;

    public WpfUrgentAttentionPreviewService()
        : this(new WpfVirtualScreenProvider())
    {
    }

    public Task ShowAsync(string message, CancellationToken cancellationToken = default)
    {
        var validation = MessageRequestValidator.Validate(
            new MessageRequest("local-preview", message));
        var messageError = validation.Errors.FirstOrDefault(error => error.Field == "Message");
        if (messageError is not null)
        {
            throw new ArgumentException(messageError.Message, nameof(message));
        }

        if (_activeWindow is not null)
        {
            _activeWindow.Activate();
            return _activeSession ?? Task.CompletedTask;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new UrgentAttentionOverlayViewModel(
            "You · local preview · nothing sent",
            message,
            PreviewTimeout,
            new SystemAsyncDelay());
        var window = new UrgentAttentionOverlayWindow(
            viewModel,
            _virtualScreenProvider.GetLayout());
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _activeWindow = window;
        _activeSession = completion.Task;
        if (Application.Current?.MainWindow is Window owner && owner.IsVisible)
        {
            window.Owner = owner;
        }

        window.Closed += OnClosed;
        var cancellationRegistration = cancellationToken.Register(
            () => window.Dispatcher.BeginInvoke(window.Close));
        try
        {
            window.Show();
        }
        catch
        {
            cancellationRegistration.Dispose();
            window.Closed -= OnClosed;
            _activeWindow = null;
            _activeSession = null;
            window.Dispose();
            throw;
        }

        return AwaitCompletionAsync(completion.Task, cancellationRegistration);

        void OnClosed(object? sender, EventArgs eventArgs)
        {
            window.Closed -= OnClosed;
            _activeWindow = null;
            _activeSession = null;
            completion.TrySetResult();
        }
    }

    private static async Task AwaitCompletionAsync(
        Task completion,
        CancellationTokenRegistration cancellationRegistration)
    {
        using (cancellationRegistration)
        {
            await completion.ConfigureAwait(true);
        }
    }
}
