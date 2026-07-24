using Unskip.App.Commands;
using Unskip.App.Services;

namespace Unskip.App.ViewModels;

internal sealed class UrgentAttentionOverlayViewModel
{
    private readonly IAsyncDelay _delay;
    private readonly TimeSpan _timeout;
    private bool _isDismissed;

    public UrgentAttentionOverlayViewModel(
        string senderLabel,
        string title,
        string message,
        TimeSpan timeout,
        IAsyncDelay delay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senderLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes(5))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                timeout.TotalSeconds,
                "The overlay timeout must be greater than zero and no more than five minutes.");
        }

        SenderLabel = senderLabel;
        Title = title;
        Message = message;
        _timeout = timeout;
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        CloseCommand = new RelayCommand(_ => Dismiss());
    }

    public event EventHandler? DismissRequested;

    public string SenderLabel { get; }

    public string Title { get; }

    public string Message { get; }

    public string TimeoutLabel => $"This local preview closes automatically after {_timeout.TotalSeconds:N0} seconds.";

    public RelayCommand CloseCommand { get; }

    public async Task WaitForTimeoutAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _delay.DelayAsync(_timeout, cancellationToken).ConfigureAwait(true);
            Dismiss();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The window was already dismissed through a user or system action.
        }
    }

    private void Dismiss()
    {
        if (_isDismissed)
        {
            return;
        }

        _isDismissed = true;
        DismissRequested?.Invoke(this, EventArgs.Empty);
    }
}
