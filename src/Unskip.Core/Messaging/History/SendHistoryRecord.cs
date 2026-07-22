using Unskip.Core.Devices;

namespace Unskip.Core.Messaging.History;

public sealed record SendHistoryRecord(
    Guid Id,
    Guid? DeviceId,
    string AliasSnapshot,
    string? ComputerNameSnapshot,
    string? Ipv4AddressSnapshot,
    DeviceDestinationKind DestinationKind,
    string DestinationSnapshot,
    MessageDeliveryStatus DeliveryStatus,
    MessageFailureCategory FailureCategory,
    DateTimeOffset OccurredAt,
    TimeSpan Duration,
    int? ExitCode,
    string? DiagnosticSummary,
    int MessageLength);
