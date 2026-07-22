namespace Unskip.Infrastructure.Windows;

internal sealed record ProcessExecutionResult(
    ProcessExecutionOutcome Outcome,
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration,
    int? NativeErrorCode = null,
    bool TerminationSucceeded = true,
    string? FailureDiagnostic = null);
