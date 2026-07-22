using Unskip.Core.Devices;

namespace Unskip.App.ViewModels;

public sealed class DeviceListItemViewModel(Device device, DateTimeOffset currentTime)
{
    public Device Device { get; } = device ?? throw new ArgumentNullException(nameof(device));

    public Guid Id => Device.Id;

    public string Alias => Device.Alias;

    public string ComputerName => Device.ComputerName ?? "No computer name";

    public string Ipv4Address => Device.Ipv4Address ?? "No IPv4 address";

    public bool IsFavorite => Device.IsFavorite;

    public string FavoriteSymbol => IsFavorite ? "★" : "☆";

    public string PreferredDestinationLabel => Device.PreferredDestination switch
    {
        DeviceDestinationKind.Hostname => "Computer name",
        DeviceDestinationKind.Ipv4 => "IPv4 address",
        _ => "Destination",
    };

    public string ResolvedDestination => Device.PreferredDestination switch
    {
        DeviceDestinationKind.Hostname => Device.ComputerName ?? string.Empty,
        DeviceDestinationKind.Ipv4 => Device.Ipv4Address ?? string.Empty,
        _ => string.Empty,
    };

    public string Description => string.IsNullOrWhiteSpace(Device.Description)
        ? "Saved device"
        : Device.Description;

    public string RecencyLabel => Device.LastUsedAt switch
    {
        null => "Not used yet",
        var timestamp when timestamp.Value >= currentTime.AddDays(-1) => "Used recently",
        var timestamp => $"Used {timestamp.Value.LocalDateTime:d}",
    };
}
