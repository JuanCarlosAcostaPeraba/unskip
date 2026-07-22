namespace Unskip.Core.Devices;

public sealed record DeviceInput(
    string? Alias,
    string? ComputerName,
    string? Ipv4Address,
    string? Description,
    bool IsFavorite = false,
    DeviceDestinationKind? PreferredDestination = null);
