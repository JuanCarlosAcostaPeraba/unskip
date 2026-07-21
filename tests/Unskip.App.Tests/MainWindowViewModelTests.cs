using Unskip.App.ViewModels;

namespace Unskip.App.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void InitialShellExposesExpectedSectionsWithoutDeliveryClaims()
    {
        var viewModel = new MainWindowViewModel();

        Assert.Equal(["Send", "Devices", "History"], viewModel.NavigationItems);
        Assert.DoesNotContain("read", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("acknowledged", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }
}
