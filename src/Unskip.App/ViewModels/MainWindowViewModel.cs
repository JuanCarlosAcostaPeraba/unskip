using Unskip.Core;

namespace Unskip.App.ViewModels;

/// <summary>
/// Provides product identity and the active device-directory workspace.
/// </summary>
public sealed class MainWindowViewModel(DeviceDirectoryViewModel deviceDirectory)
{
    public string ProductName { get; } = ProductIdentity.Name;

    public string Tagline { get; } = ProductIdentity.Tagline;

    public string AffiliationNotice { get; } = ProductIdentity.AffiliationNotice;

    public string CurrentSection { get; } = "Devices";

    public string SectionDescription { get; } = "Choose a saved device or prepare a one-time destination.";

    public IReadOnlyList<NavigationItemViewModel> NavigationItems { get; } =
    [
        new("Send", "↗", false, "Next"),
        new("Devices", "◫", true),
        new("History", "◷", false, "Later"),
    ];

    public DeviceDirectoryViewModel DeviceDirectory { get; } = deviceDirectory
        ?? throw new ArgumentNullException(nameof(deviceDirectory));

    public Task InitializeAsync()
    {
        return DeviceDirectory.InitializeAsync();
    }
}
