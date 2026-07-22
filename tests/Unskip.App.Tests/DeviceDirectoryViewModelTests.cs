using Unskip.Core.Devices;

namespace Unskip.App.Tests;

public sealed class DeviceDirectoryViewModelTests
{
    [Theory]
    [InlineData("Joan")]
    [InlineData("chuc159")]
    [InlineData("10.198")]
    public async Task SearchFindsSameDeviceAcrossEveryField(string search)
    {
        var device = ViewModelTestContext.Device(
            "Joan",
            "chuc159",
            "10.198.198.4",
            description: "Fictitious test device");
        var context = ViewModelTestContext.Create(device);
        await context.Main.InitializeAsync();

        context.Directory.SearchText = search;

        var result = Assert.Single(context.Directory.FilteredDevices);
        Assert.Equal(device.Id, result.Id);
    }

    [Fact]
    public async Task SelectingSavedDeviceShowsAliasAndPreferredTechnicalDestination()
    {
        var device = ViewModelTestContext.Device(
            "Shared workstation",
            "shared-07",
            "192.0.2.7",
            DeviceDestinationKind.Ipv4);
        var context = ViewModelTestContext.Create(device);
        await context.Main.InitializeAsync();

        context.Directory.SelectedDevice = Assert.Single(context.Directory.FilteredDevices);

        Assert.Equal("Shared workstation", context.Directory.PreparedAlias);
        Assert.Equal("192.0.2.7", context.Directory.PreparedDestination);
        Assert.Equal("IPv4 address", context.Directory.PreparedKindLabel);
        Assert.Equal(DeviceDestinationKind.Ipv4, context.Directory.PreparedDestinationKind);
        Assert.Equal(device.Id, context.Directory.PreparedDeviceId);
    }

    [Theory]
    [InlineData("workstation-7", "workstation-7", "Computer name")]
    [InlineData("192.0.2.7", "192.0.2.7", "IPv4 address")]
    public async Task ManualDestinationsRemainUsableWithoutSaving(
        string input,
        string expectedDestination,
        string expectedKind)
    {
        var context = ViewModelTestContext.Create();
        context.Directory.ManualDestination = input;

        context.Directory.UseManualDestinationCommand.Execute(null);

        Assert.True(context.Directory.IsManualPrepared);
        Assert.Equal("Manual destination", context.Directory.PreparedAlias);
        Assert.Equal(expectedDestination, context.Directory.PreparedDestination);
        Assert.Equal(expectedKind, context.Directory.PreparedKindLabel);
        Assert.Null(context.Directory.PreparedDeviceId);
        Assert.Empty(await context.Repository.GetAllAsync());
    }

    [Fact]
    public void InvalidManualIpv4DisplaysUnderstandableError()
    {
        var context = ViewModelTestContext.Create();
        context.Directory.ManualDestination = "999.0.0.1";

        context.Directory.UseManualDestinationCommand.Execute(null);

        Assert.False(context.Directory.HasPreparedDestination);
        Assert.Contains("IPv4", context.Directory.ManualDestinationError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateMapsValidationErrorsNextToFieldsThenSavesValidDevice()
    {
        var context = ViewModelTestContext.Create();
        await context.Main.InitializeAsync();
        context.Directory.NewDeviceCommand.Execute(null);

        await context.Directory.SaveDeviceCommand.ExecuteAsync();

        Assert.True(context.Directory.Editor.IsOpen);
        Assert.NotNull(context.Directory.Editor.AliasError);
        Assert.NotNull(context.Directory.Editor.DestinationError);

        context.Directory.Editor.Alias = "Reception";
        context.Directory.Editor.ComputerName = "front-desk";
        await context.Directory.SaveDeviceCommand.ExecuteAsync();

        Assert.False(context.Directory.Editor.IsOpen);
        Assert.Equal("Reception", Assert.Single(context.Directory.FilteredDevices).Alias);
        Assert.Equal("front-desk", context.Directory.PreparedDestination);
    }

    [Fact]
    public async Task EditAndFavoriteCommandsUpdateSelectedDevice()
    {
        var device = ViewModelTestContext.Device("Reception", "front-desk", null);
        var context = ViewModelTestContext.Create(device);
        await context.Main.InitializeAsync();
        context.Directory.SelectedDevice = Assert.Single(context.Directory.FilteredDevices);
        context.Directory.EditDeviceCommand.Execute(null);
        context.Directory.Editor.Alias = "Main reception";

        await context.Directory.SaveDeviceCommand.ExecuteAsync();
        await context.Directory.ToggleFavoriteCommand.ExecuteAsync(context.Directory.SelectedDevice);

        Assert.Equal("Main reception", context.Directory.SelectedDevice!.Alias);
        Assert.True(context.Directory.SelectedDevice.IsFavorite);
    }

    [Fact]
    public async Task DescriptionValidationAppearsBesideDescriptionField()
    {
        var context = ViewModelTestContext.Create();
        await context.Main.InitializeAsync();
        context.Directory.NewDeviceCommand.Execute(null);
        context.Directory.Editor.Alias = "Reception";
        context.Directory.Editor.ComputerName = "front-desk";
        context.Directory.Editor.Description = new string('d', DevicePolicy.MaximumDescriptionLength + 1);

        await context.Directory.SaveDeviceCommand.ExecuteAsync();

        Assert.NotNull(context.Directory.Editor.DescriptionError);
        Assert.Null(context.Directory.Editor.GeneralError);
    }

    [Fact]
    public async Task DeleteRequiresExplicitConfirmation()
    {
        var device = ViewModelTestContext.Device("Reception", "front-desk", null);
        var context = ViewModelTestContext.Create(device);
        await context.Main.InitializeAsync();
        context.Directory.SelectedDevice = Assert.Single(context.Directory.FilteredDevices);

        context.Confirmation.Response = false;
        await context.Directory.DeleteDeviceCommand.ExecuteAsync();
        Assert.Single(context.Directory.FilteredDevices);

        context.Confirmation.Response = true;
        await context.Directory.DeleteDeviceCommand.ExecuteAsync();

        Assert.Empty(context.Directory.FilteredDevices);
        Assert.False(context.Directory.HasPreparedDestination);
        Assert.Equal(2, context.Confirmation.RequestCount);
    }

    [Fact]
    public void ManualDestinationCanBePromotedToEditorWithoutBeingSavedAutomatically()
    {
        var context = ViewModelTestContext.Create();
        context.Directory.ManualDestination = "workstation-7";
        context.Directory.UseManualDestinationCommand.Execute(null);

        context.Directory.SaveManualDestinationCommand.Execute(null);

        Assert.True(context.Directory.Editor.IsOpen);
        Assert.Equal("workstation-7", context.Directory.Editor.ComputerName);
        Assert.Null(context.Directory.Editor.Alias);
    }

    [Fact]
    public async Task PrepareMessageOpensComposerWithoutClaimingDelivery()
    {
        var context = ViewModelTestContext.Create(
            ViewModelTestContext.Device("Reception", "front-desk", null));
        await context.Main.InitializeAsync();
        context.Directory.SelectedDevice = Assert.Single(context.Directory.FilteredDevices);

        context.Directory.PrepareMessageCommand.Execute(null);

        Assert.True(context.Main.IsComposerVisible);
        Assert.Equal("Reception", context.Main.Composer.DestinationAlias);
        Assert.Equal("front-desk", context.Main.Composer.Destination);
        Assert.Contains("Nothing has been sent", context.Directory.StatusMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("read", context.Directory.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("acknowledged", context.Directory.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }
}
