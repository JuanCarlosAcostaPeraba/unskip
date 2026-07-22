using System.Diagnostics;

namespace Unskip.Infrastructure.Windows;

internal interface IProcessInvoker
{
    Task<ProcessExecutionResult> ExecuteAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
