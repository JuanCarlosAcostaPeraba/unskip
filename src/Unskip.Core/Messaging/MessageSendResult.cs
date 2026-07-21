namespace Unskip.Core.Messaging;

public sealed record MessageSendResult(
    MessageDeliveryStatus Status,
    MessageFailureCategory FailureCategory,
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration,
    string UserMessage);
