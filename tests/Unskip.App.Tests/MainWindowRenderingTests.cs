using System.Windows;

namespace Unskip.App.Tests;

public sealed class MainWindowRenderingTests
{
    [Fact]
    public void SavedDeviceTemplateCanBeMaterialized()
    {
        Exception? renderingException = null;
        var completed = false;
        var thread = new Thread(() =>
        {
            MainWindow? window = null;
            try
            {
                var device = ViewModelTestContext.Device(
                    "Studio workstation",
                    "studio-pc",
                    "192.0.2.40");
                var context = ViewModelTestContext.Create(device);
                context.Main.InitializeAsync().GetAwaiter().GetResult();

                window = new MainWindow(context.Main);
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

        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "The WPF rendering thread did not finish.");
        Assert.Null(renderingException);
        Assert.True(completed);
    }
}
