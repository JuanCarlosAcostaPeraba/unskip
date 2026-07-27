using Unskip.App.ViewModels;
using Unskip.Core.Devices;
using Unskip.Core.Messaging.History;

namespace Unskip.App.Tests;

public sealed class QuickSendViewModelTests
{
    [Fact]
    public async Task SavedDevicesLoadFavoritesFirstAndPrepareTheSelectedDestination()
    {
        var favorite = ViewModelTestContext.Device(
            "Favorite lab",
            "favorite-lab",
            null,
            isFavorite: true);
        var regular = ViewModelTestContext.Device(
            "Regular lab",
            "regular-lab",
            null);
        var context = CreateContext(regular, favorite);

        await context.ViewModel.ReloadAsync();
        context.ViewModel.SelectedDevice = context.ViewModel.Devices[0];

        Assert.Equal(favorite.Id, context.ViewModel.Devices[0].Id);
        Assert.Equal("favorite-lab", context.Composer.Destination);
        Assert.Equal(favorite.Id, context.Composer.DeviceId);
    }

    [Fact]
    public void CanonicalOneTimeIpv4PreparesAnHonestTechnicalDestination()
    {
        var context = CreateContext();
        context.ViewModel.ManualDestination = "192.0.2.44";

        context.ViewModel.UseManualDestinationCommand.Execute(null);

        Assert.Null(context.ViewModel.DestinationError);
        Assert.Equal("192.0.2.44", context.Composer.Destination);
        Assert.Equal("IPv4 address", context.Composer.DestinationKindLabel);
        Assert.Null(context.Composer.DeviceId);
    }

    [Fact]
    public void InvalidOneTimeDestinationClearsPreviouslyPreparedTarget()
    {
        var context = CreateContext();
        context.ViewModel.ManualDestination = "valid-host";
        context.ViewModel.UseManualDestinationCommand.Execute(null);
        context.ViewModel.ManualDestination = "999.2.3.4";

        context.ViewModel.UseManualDestinationCommand.Execute(null);

        Assert.Contains("canonical", context.ViewModel.DestinationError, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(context.Composer.Destination);
        Assert.False(context.Composer.SendCommand.CanExecute(null));
    }

    [Fact]
    public async Task QuickSendUsesExistingComposerAndNeverClaimsTheMessageWasRead()
    {
        var context = CreateContext();
        context.ViewModel.ManualDestination = "example-pc";
        context.ViewModel.UseManualDestinationCommand.Execute(null);
        context.Composer.Message = "Fictitious quick message";

        await context.Composer.SendCommand.ExecuteAsync();

        Assert.Equal("Sent", context.Composer.StatusLabel);
        Assert.Contains("does not prove", context.Composer.ResultMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("was read", context.Composer.ResultMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReloadingTheReusablePanelKeepsItsLocalDraft()
    {
        var context = CreateContext();
        context.Composer.Message = "Draft kept only in memory";

        await context.ViewModel.ReloadAsync();
        await context.ViewModel.ReloadAsync();

        Assert.Equal("Draft kept only in memory", context.Composer.Message);
    }

    private static TestContext CreateContext(params Device[] devices)
    {
        var baseContext = ViewModelTestContext.Create(devices);
        var directory = new DeviceDirectoryService(baseContext.Repository, baseContext.Clock);
        var history = new SendHistoryService(baseContext.HistoryRepository, baseContext.Clock);
        var composer = new MessageComposerViewModel(
            baseContext.Sender,
            history,
            baseContext.UrgentAttentionPreview);
        var viewModel = new QuickSendViewModel(directory, baseContext.Clock, composer);
        return new TestContext(viewModel, composer);
    }

    private sealed record TestContext(
        QuickSendViewModel ViewModel,
        MessageComposerViewModel Composer);
}
