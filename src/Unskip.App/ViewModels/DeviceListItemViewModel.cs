using Unskip.App.Localization;
using Unskip.Core.Devices;

namespace Unskip.App.ViewModels;

public sealed class DeviceListItemViewModel(Device device, DateTimeOffset currentTime)
{
    public Device Device { get; } = device ?? throw new ArgumentNullException(nameof(device));

    public Guid Id => Device.Id;

    public string Alias => Device.Alias;

    public string ComputerName => Device.ComputerName ?? UiText.Get("NoComputerName");

    public string Ipv4Address => Device.Ipv4Address ?? UiText.Get("NoIpv4Address");

    public bool IsFavorite => Device.IsFavorite;

    public string FavoriteSymbol => IsFavorite ? "★" : "☆";

    public string PreferredDestinationLabel => Device.PreferredDestination switch
    {
        DeviceDestinationKind.Hostname => UiText.Get("ComputerName"),
        DeviceDestinationKind.Ipv4 => UiText.Get("Ipv4Address"),
        _ => UiText.Get("Destination"),
    };

    public string ResolvedDestination => Device.PreferredDestination switch
    {
        DeviceDestinationKind.Hostname => Device.ComputerName ?? string.Empty,
        DeviceDestinationKind.Ipv4 => Device.Ipv4Address ?? string.Empty,
        _ => string.Empty,
    };

    public string Description => string.IsNullOrWhiteSpace(Device.Description)
        ? UiText.Get("SavedDevice")
        : Device.Description;

    public string RecencyLabel => Device.LastUsedAt switch
    {
        null => UiText.Get("NotUsedYet"),
        var timestamp when timestamp.Value >= currentTime.AddDays(-1) => UiText.Get("UsedRecently"),
        var timestamp => UiText.Format("UsedOn", timestamp.Value.LocalDateTime),
    };
}
