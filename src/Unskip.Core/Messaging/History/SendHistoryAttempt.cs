using Unskip.Core.Devices;

namespace Unskip.Core.Messaging.History;

public sealed record SendHistoryAttempt(
    Guid? DeviceId,
    string AliasSnapshot,
    string? ComputerNameSnapshot,
    string? Ipv4AddressSnapshot,
    DeviceDestinationKind DestinationKind,
    string DestinationSnapshot,
    MessageDeliveryStatus DeliveryStatus,
    MessageFailureCategory FailureCategory,
    TimeSpan Duration,
    int? ExitCode,
    string? DiagnosticSummary,
    int MessageLength);
