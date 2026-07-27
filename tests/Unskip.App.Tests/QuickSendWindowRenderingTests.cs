using Unskip.App.ViewModels;
using Unskip.App.Views;
using Unskip.Core.Devices;
using Unskip.Core.Messaging.History;

namespace Unskip.App.Tests;

public sealed class QuickSendWindowRenderingTests
{
    [Fact]
    public void LocalizedPanelWithSavedDeviceCanBeMaterialized()
    {
        Exception? renderingException = null;
        var completed = false;
        var thread = new Thread(() =>
        {
            QuickSendWindow? window = null;
            try
            {
                var device = ViewModelTestContext.Device(
                    "Studio workstation",
                    "studio-pc",
                    "192.0.2.40");
                var context = ViewModelTestContext.Create(device);
                var directory = new DeviceDirectoryService(context.Repository, context.Clock);
                var history = new SendHistoryService(context.HistoryRepository, context.Clock);
                var composer = new MessageComposerViewModel(
                    context.Sender,
                    history,
                    context.UrgentAttentionPreview);
                var viewModel = new QuickSendViewModel(directory, context.Clock, composer);
                viewModel.ReloadAsync().GetAwaiter().GetResult();

                window = new QuickSendWindow(viewModel);
                window.Show();
                window.UpdateLayout();
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

        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "The quick-send rendering thread did not finish.");
        Assert.Null(renderingException);
        Assert.True(completed);
    }
}
