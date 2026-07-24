using Unskip.App.Services;
using Unskip.App.ViewModels;

namespace Unskip.App.Tests;

public sealed class UrgentAttentionOverlayViewModelTests
{
    [Fact]
    public void CloseCommandRequestsDismissalOnlyOnce()
    {
        var viewModel = CreateViewModel(new CompletedDelay());
        var dismissalCount = 0;
        viewModel.DismissRequested += (_, _) => dismissalCount++;

        viewModel.CloseCommand.Execute(null);
        viewModel.CloseCommand.Execute(null);

        Assert.Equal(1, dismissalCount);
    }

    [Fact]
    public async Task TimeoutRequestsDismissal()
    {
        var delay = new ControlledDelay();
        var viewModel = CreateViewModel(delay);
        var dismissalCount = 0;
        viewModel.DismissRequested += (_, _) => dismissalCount++;

        var timeout = viewModel.WaitForTimeoutAsync(CancellationToken.None);
        delay.Complete();
        await timeout;

        Assert.Equal(1, dismissalCount);
    }

    [Fact]
    public async Task CancellationDoesNotRequestDismissal()
    {
        var viewModel = CreateViewModel(new CancellationDelay());
        var dismissalCount = 0;
        viewModel.DismissRequested += (_, _) => dismissalCount++;
        using var cancellationSource = new CancellationTokenSource();

        var timeout = viewModel.WaitForTimeoutAsync(cancellationSource.Token);
        cancellationSource.Cancel();
        await timeout;

        Assert.Equal(0, dismissalCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(301)]
    public void InvalidTimeoutIsRejected(int seconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new UrgentAttentionOverlayViewModel(
                "Local preview",
                "Urgent preview",
                "Fictitious local preview",
                TimeSpan.FromSeconds(seconds),
                new CompletedDelay()));
    }

    private static UrgentAttentionOverlayViewModel CreateViewModel(IAsyncDelay delay)
    {
        return new UrgentAttentionOverlayViewModel(
            "Local preview",
            "Urgent preview",
            "Fictitious local preview",
            TimeSpan.FromSeconds(60),
            delay);
    }

    private sealed class CompletedDelay : IAsyncDelay
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ControlledDelay : IAsyncDelay
    {
        private readonly TaskCompletionSource _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return _completion.Task;
        }

        public void Complete()
        {
            _completion.SetResult();
        }
    }

    private sealed class CancellationDelay : IAsyncDelay
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }
}
