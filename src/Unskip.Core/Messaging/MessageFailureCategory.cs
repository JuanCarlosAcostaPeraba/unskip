namespace Unskip.Core.Messaging;

public enum MessageFailureCategory
{
    None,
    Validation,
    PermissionDenied,
    TargetUnavailable,
    NativeRejected,
    Timeout,
    Cancelled,
    ExecutableUnavailable,
    ProcessFailure,
    ProcessTerminationFailure,
}
