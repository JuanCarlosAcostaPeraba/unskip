using System.Windows.Documents;

namespace Unskip.App.Tests;

public sealed class MainWindowAttributionTests
{
    [Fact]
    public void DeveloperCommandOpensFixedPortfolioUri()
    {
        var context = ViewModelTestContext.Create();

        context.Main.OpenDeveloperPortfolioCommand.Execute(null);

        Assert.Equal(new Uri("https://jcap.tech/"), context.ExternalUriLauncher.OpenedUri);
        Assert.Empty(context.Main.DeveloperLinkStatus);
    }

    [Fact]
    public void DeveloperCommandReportsBrowserLaunchFailure()
    {
        var context = ViewModelTestContext.Create();
        context.ExternalUriLauncher.Result = false;

        context.Main.OpenDeveloperPortfolioCommand.Execute(null);

        Assert.Contains("jcap.tech", context.Main.DeveloperLinkStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void DeveloperPortfolioIsRenderedAsAKeyboardAccessibleHyperlink()
    {
        Exception? renderingException = null;
        var portfolioLinkFound = false;
        var portfolioLinkFocusable = false;
        var portfolioLinkHasCommand = false;
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

                var portfolioLink = window.FindName("DeveloperPortfolioLink") as Hyperlink;
                portfolioLinkFound = portfolioLink is not null;
                portfolioLinkFocusable = portfolioLink?.Focusable == true;
                portfolioLinkHasCommand = portfolioLink?.Command is not null;
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
        Assert.True(portfolioLinkFound);
        Assert.True(portfolioLinkFocusable);
        Assert.True(portfolioLinkHasCommand);
    }
}
