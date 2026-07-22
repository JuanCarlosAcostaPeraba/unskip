using System.ComponentModel;
using System.Diagnostics;

namespace Unskip.Infrastructure.Windows;

internal sealed class SystemProcessInvoker : IProcessInvoker
{
    public async Task<ProcessExecutionResult> ExecuteAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        var stopwatch = Stopwatch.StartNew();
        if (cancellationToken.IsCancellationRequested)
        {
            return EmptyResult(ProcessExecutionOutcome.Cancelled, stopwatch.Elapsed);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                return EmptyResult(
                    ProcessExecutionOutcome.StartFailed,
                    stopwatch.Elapsed,
                    failureDiagnostic: "The operating system did not start msg.exe.");
            }
        }
        catch (Win32Exception exception)
        {
            return EmptyResult(
                ProcessExecutionOutcome.StartFailed,
                stopwatch.Elapsed,
                exception.NativeErrorCode,
                failureDiagnostic: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return EmptyResult(
                ProcessExecutionOutcome.StartFailed,
                stopwatch.Elapsed,
                failureDiagnostic: exception.Message);
        }

        // Continue draining redirected streams after timeout/cancellation so the child cannot block on full pipes.
        var standardOutputTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var standardErrorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(linkedSource.Token).ConfigureAwait(false);
            var streams = await ReadStreamsAsync(standardOutputTask, standardErrorTask).ConfigureAwait(false);
            return new ProcessExecutionResult(
                ProcessExecutionOutcome.Completed,
                process.ExitCode,
                streams.StandardOutput,
                streams.StandardError,
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (linkedSource.IsCancellationRequested)
        {
            var outcome = cancellationToken.IsCancellationRequested
                ? ProcessExecutionOutcome.Cancelled
                : ProcessExecutionOutcome.TimedOut;

            var terminationSucceeded = TryTerminate(process, out var terminationDiagnostic);
            if (!terminationSucceeded)
            {
                return EmptyResult(
                    outcome,
                    stopwatch.Elapsed,
                    terminationSucceeded: false,
                    failureDiagnostic: terminationDiagnostic);
            }

            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            var streams = await ReadStreamsAsync(standardOutputTask, standardErrorTask).ConfigureAwait(false);
            return new ProcessExecutionResult(
                outcome,
                process.ExitCode,
                streams.StandardOutput,
                streams.StandardError,
                stopwatch.Elapsed);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or IOException)
        {
            var terminationSucceeded = TryTerminate(process, out var terminationDiagnostic);
            var diagnostic = terminationSucceeded
                ? exception.Message
                : string.Concat(exception.Message, " ", terminationDiagnostic);

            if (terminationSucceeded)
            {
                try
                {
                    await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (InvalidOperationException)
                {
                    // The process already exited between the termination check and the wait.
                }
            }

            return EmptyResult(
                ProcessExecutionOutcome.ExecutionFailed,
                stopwatch.Elapsed,
                terminationSucceeded: terminationSucceeded,
                failureDiagnostic: diagnostic);
        }
    }

    private static async Task<(string StandardOutput, string StandardError)> ReadStreamsAsync(
        Task<string> standardOutputTask,
        Task<string> standardErrorTask)
    {
        await Task.WhenAll(standardOutputTask, standardErrorTask).ConfigureAwait(false);
        return (standardOutputTask.Result, standardErrorTask.Result);
    }

    private static bool TryTerminate(Process process, out string? failureDiagnostic)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            failureDiagnostic = null;
            return true;
        }
        catch (InvalidOperationException)
        {
            failureDiagnostic = null;
            return true;
        }
        catch (Win32Exception exception)
        {
            failureDiagnostic = exception.Message;
            return false;
        }
    }

    private static ProcessExecutionResult EmptyResult(
        ProcessExecutionOutcome outcome,
        TimeSpan duration,
        int? nativeErrorCode = null,
        bool terminationSucceeded = true,
        string? failureDiagnostic = null)
    {
        return new ProcessExecutionResult(
            outcome,
            null,
            string.Empty,
            string.Empty,
            duration,
            nativeErrorCode,
            terminationSucceeded,
            failureDiagnostic);
    }
}
