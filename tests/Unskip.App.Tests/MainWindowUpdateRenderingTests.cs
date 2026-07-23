using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Unskip.App.Tests;

public sealed class MainWindowUpdateRenderingTests
{
    [Fact]
    public void UpdateActionIsRenderedInMainWindow()
    {
        Exception? renderingException = null;
        var updateActionFound = false;
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

                updateActionFound = FindVisualDescendants<Button>(window)
                    .Any(button => string.Equals(
                        button.Content?.ToString(),
                        "Check for updates",
                        StringComparison.Ordinal));
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
        Assert.True(updateActionFound);
    }

    private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindVisualDescendants<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
