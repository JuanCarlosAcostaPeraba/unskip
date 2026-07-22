namespace Unskip.App.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void DevicesIsActiveWithoutDeliveryClaims()
    {
        var context = ViewModelTestContext.Create();

        Assert.Equal("Devices", context.Main.CurrentSection);
        Assert.Equal(["Send", "Devices", "History"], context.Main.NavigationItems.Select(item => item.Label));
        Assert.True(context.Main.NavigationItems.Single(item => item.Label == "Devices").IsActive);
        Assert.DoesNotContain("read", context.Main.SectionDescription, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("acknowledged", context.Main.SectionDescription, StringComparison.OrdinalIgnoreCase);
    }
}
