namespace Unskip.Core.Devices;

public sealed record ValidatedDeviceInput(
    string Alias,
    string AliasKey,
    string? ComputerName,
    string? Ipv4Address,
    string? Description,
    bool IsFavorite,
    DeviceDestinationKind PreferredDestination);
