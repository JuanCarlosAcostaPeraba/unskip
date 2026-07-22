using Unskip.Core.Devices;

namespace Unskip.Infrastructure.Persistence;

internal sealed class DeviceEntity
{
    public Guid Id { get; set; }

    public required string Alias { get; set; }

    public required string AliasKey { get; set; }

    public string? ComputerName { get; set; }

    public string? Ipv4Address { get; set; }

    public string? Description { get; set; }

    public bool IsFavorite { get; set; }

    public DeviceDestinationKind PreferredDestination { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public ICollection<SendHistoryEntity> SendHistoryRecords { get; } = [];
}
