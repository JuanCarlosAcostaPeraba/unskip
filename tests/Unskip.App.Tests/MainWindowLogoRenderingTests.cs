using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Unskip.App.Tests;

public sealed class MainWindowLogoRenderingTests
{
    [Fact]
    public void SidebarLogoUsesVectorDrawingInsteadOfRasterIcon()
    {
        Exception? renderingException = null;
        var logoFound = false;
        var usesVectorDrawing = false;
        var usesBitmapSource = false;
        var thread = new Thread(() =>
        {
            MainWindow? window = null;
            try
            {
                var context = ViewModelTestContext.Create();
                context.Main.InitializeAsync().GetAwaiter().GetResult();
                window = new MainWindow(context.Main);
                window.Show();
                window.UpdateLayout();

                var logo = window.FindName("SidebarLogo") as Image;
                logoFound = logo is not null;
                usesVectorDrawing = logo?.Source is DrawingImage;
                usesBitmapSource = logo?.Source is BitmapSource;
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
        Assert.True(logoFound);
        Assert.True(usesVectorDrawing);
        Assert.False(usesBitmapSource);
    }
}
