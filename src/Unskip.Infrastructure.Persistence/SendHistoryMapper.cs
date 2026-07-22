using Unskip.Core.Messaging.History;

namespace Unskip.Infrastructure.Persistence;

internal static class SendHistoryMapper
{
    public static SendHistoryRecord ToDomain(SendHistoryEntity entity) => new(
        entity.Id, entity.DeviceId, entity.AliasSnapshot, entity.ComputerNameSnapshot,
        entity.Ipv4AddressSnapshot, entity.DestinationKind, entity.DestinationSnapshot,
        entity.DeliveryStatus, entity.FailureCategory, entity.OccurredAt,
        TimeSpan.FromTicks(entity.DurationTicks), entity.ExitCode,
        entity.DiagnosticSummary, entity.MessageLength);

    public static SendHistoryEntity ToEntity(SendHistoryRecord record) => new()
    {
        Id = record.Id,
        DeviceId = record.DeviceId,
        AliasSnapshot = record.AliasSnapshot,
        ComputerNameSnapshot = record.ComputerNameSnapshot,
        Ipv4AddressSnapshot = record.Ipv4AddressSnapshot,
        DestinationKind = record.DestinationKind,
        DestinationSnapshot = record.DestinationSnapshot,
        DeliveryStatus = record.DeliveryStatus,
        FailureCategory = record.FailureCategory,
        OccurredAt = record.OccurredAt,
        DurationTicks = record.Duration.Ticks,
        ExitCode = record.ExitCode,
        DiagnosticSummary = record.DiagnosticSummary,
        MessageLength = record.MessageLength,
    };
}
