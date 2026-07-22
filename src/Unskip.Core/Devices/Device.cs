namespace Unskip.Core.Devices;

public sealed record Device(
    Guid Id,
    string Alias,
    string AliasKey,
    string? ComputerName,
    string? Ipv4Address,
    string? Description,
    bool IsFavorite,
    DeviceDestinationKind PreferredDestination,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastUsedAt);
