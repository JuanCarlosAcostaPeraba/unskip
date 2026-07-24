using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using Unskip.App.Services;
using Unskip.App.ViewModels;
using Unskip.App.Views;

namespace Unskip.App.Tests;

public sealed class UrgentAttentionOverlayRenderingTests
{
    [Fact]
    public void OverlayCoversVirtualScreenAndKeepsPredictableDismissalControls()
    {
        Exception? renderingException = null;
        var completed = false;
        var thread = new Thread(() =>
        {
            UrgentAttentionOverlayWindow? window = null;
            try
            {
                var viewModel = new UrgentAttentionOverlayViewModel(
                    "Local preview",
                    "Urgent preview",
                    "Fictitious local preview message",
                    TimeSpan.FromSeconds(60),
                    new PendingDelay());
                var bounds = new VirtualScreenBounds(-1280, -120, 3000, 900);
                window = new UrgentAttentionOverlayWindow(viewModel, bounds);

                window.Show();
                window.UpdateLayout();

                Assert.Equal(bounds.Left, window.Left);
                Assert.Equal(bounds.Top, window.Top);
                Assert.Equal(bounds.Width, window.Width);
                Assert.Equal(bounds.Height, window.Height);
                Assert.True(window.Topmost);
                Assert.Equal(WindowStyle.None, window.WindowStyle);
                Assert.Equal(ResizeMode.NoResize, window.ResizeMode);
                Assert.False(window.ShowInTaskbar);

                var closeButton = Assert.IsType<Button>(window.FindName("CloseButton"));
                Assert.Equal(
                    "Close urgent message preview",
                    AutomationProperties.GetName(closeButton));
                Assert.True(closeButton.IsDefault);
                Assert.Contains(
                    window.InputBindings.OfType<KeyBinding>(),
                    binding => binding.Key == Key.Escape);
                Assert.Contains(
                    window.InputBindings.OfType<KeyBinding>(),
                    binding => binding.Key == Key.F4 && binding.Modifiers == ModifierKeys.Alt);
                completed = true;
            }
            catch (Exception exception)
            {
                renderingException = exception;
            }
            finally
            {
                window?.Close();
            }
        })
        {
            IsBackground = true,
        };
        thread.SetApartmentState(ApartmentState.STA);

        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "The overlay rendering thread did not finish.");
        Assert.Null(renderingException);
        Assert.True(completed);
    }

    [Fact]
    public void VirtualScreenBoundsRejectInvalidGeometry()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new VirtualScreenBounds(0, 0, 0, 1080));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new VirtualScreenBounds(double.NaN, 0, 1920, 1080));
    }

    private sealed class PendingDelay : IAsyncDelay
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }
}
