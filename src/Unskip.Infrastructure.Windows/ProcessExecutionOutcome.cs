namespace Unskip.Infrastructure.Windows;

internal enum ProcessExecutionOutcome
{
    Completed,
    TimedOut,
    Cancelled,
    StartFailed,
    ExecutionFailed,
}
