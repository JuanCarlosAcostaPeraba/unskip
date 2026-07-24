using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Unskip.App.Services;
using Unskip.App.ViewModels;
using Unskip.App.Views;
using Unskip.Core.Messaging;

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
                var bounds = new VirtualScreenBounds(-1280, -120, 3000, 1020);
                var attentionBounds = new VirtualScreenBounds(0, 0, 1720, 900);
                window = new UrgentAttentionOverlayWindow(
                    viewModel,
                    new VirtualScreenLayout(bounds, attentionBounds));

                Assert.Equal(bounds.Left, window.Left);
                Assert.Equal(bounds.Top, window.Top);
                Assert.Equal(bounds.Width, window.Width);
                Assert.Equal(bounds.Height, window.Height);
                var transform = Assert.IsType<TranslateTransform>(
                    window.FindName("MessageCard") is Border card
                        ? card.RenderTransform
                        : null);
                Assert.Equal(640, transform.X);
                Assert.Equal(60, transform.Y);

                window.Show();
                window.UpdateLayout();

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

    [Fact]
    public void VirtualScreenLayoutRejectsAttentionAreaOutsideDesktop()
    {
        var desktop = new VirtualScreenBounds(-1280, 0, 3200, 1080);
        var outsideDesktop = new VirtualScreenBounds(0, 0, 2560, 1080);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new VirtualScreenLayout(desktop, outsideDesktop));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Visible\u0001hidden")]
    public async Task PreviewServiceRejectsInvalidMessageBeforeReadingDisplayGeometry(string message)
    {
        var service = new WpfUrgentAttentionPreviewService(new UnexpectedScreenProvider());

        await Assert.ThrowsAsync<ArgumentException>(() => service.ShowAsync(message));
    }

    [Fact]
    public async Task PreviewServiceRejectsOversizedMessageBeforeReadingDisplayGeometry()
    {
        var service = new WpfUrgentAttentionPreviewService(new UnexpectedScreenProvider());
        var message = new string('x', MessagePolicy.MaximumMessageLength + 1);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ShowAsync(message));
    }

    private sealed class PendingDelay : IAsyncDelay
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class UnexpectedScreenProvider : IVirtualScreenProvider
    {
        public VirtualScreenLayout GetLayout()
        {
            throw new InvalidOperationException("Display geometry should not be read for invalid content.");
        }
    }
}
