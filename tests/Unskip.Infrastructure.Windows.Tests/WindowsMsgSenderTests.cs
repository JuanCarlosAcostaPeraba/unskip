using System.Diagnostics;
using Unskip.Core.Messaging;

namespace Unskip.Infrastructure.Windows.Tests;

public sealed class WindowsMsgSenderTests
{
    [Fact]
    public async Task InvalidTargetIsRejectedBeforeProcessExecution()
    {
        var invoker = new RecordingProcessInvoker(Completed(exitCode: 0));
        var sender = CreateSender(invoker);

        var result = await sender.SendAsync(new MessageRequest("server&whoami", "Test message"));

        Assert.Equal(MessageDeliveryStatus.Rejected, result.Status);
        Assert.Equal(MessageFailureCategory.Validation, result.FailureCategory);
        Assert.Equal(0, invoker.InvocationCount);
    }

    [Fact]
    public async Task ZeroExitCodeMapsToSentWithoutReadClaim()
    {
        var sender = CreateSender(new RecordingProcessInvoker(Completed(exitCode: 0)));

        var result = await sender.SendAsync(new MessageRequest("desktop-01", "Test message"));

        Assert.Equal(MessageDeliveryStatus.Sent, result.Status);
        Assert.Equal(MessageFailureCategory.None, result.FailureCategory);
        Assert.Contains("does not confirm", result.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Ipv4DestinationUsesResolvedComputerName()
    {
        var invoker = new RecordingProcessInvoker(Completed(exitCode: 0));
        var sender = CreateSender(
            invoker,
            new StubServerResolver(WindowsMsgServerResolution.Success("host-25.example.test")));

        var result = await sender.SendAsync(new MessageRequest("192.0.2.25", "Test message"));

        Assert.Equal(MessageDeliveryStatus.Sent, result.Status);
        Assert.Equal(
            ["*", "/SERVER:host-25.example.test", "Test message"],
            invoker.LastStartInfo!.ArgumentList);
    }

    [Fact]
    public async Task ResolutionFailureDoesNotStartProcess()
    {
        var invoker = new RecordingProcessInvoker(Completed(exitCode: 0));
        var sender = CreateSender(
            invoker,
            new StubServerResolver(WindowsMsgServerResolution.Failure("DNS verification failed.")));

        var result = await sender.SendAsync(new MessageRequest("192.0.2.25", "Test message"));

        Assert.Equal(MessageDeliveryStatus.Failed, result.Status);
        Assert.Equal(MessageFailureCategory.TargetUnavailable, result.FailureCategory);
        Assert.Contains("computer name", result.UserMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("DNS verification failed.", result.StandardError);
        Assert.Equal(0, invoker.InvocationCount);
    }

    [Fact]
    public async Task CancellationDuringResolutionDoesNotStartProcess()
    {
        var invoker = new RecordingProcessInvoker(Completed(exitCode: 0));
        using var cancellationSource = new CancellationTokenSource();
        var sender = CreateSender(
            invoker,
            new CancellingServerResolver(cancellationSource));

        var result = await sender.SendAsync(
            new MessageRequest("192.0.2.25", "Test message"),
            cancellationSource.Token);

        Assert.Equal(MessageDeliveryStatus.Cancelled, result.Status);
        Assert.Equal(MessageFailureCategory.Cancelled, result.FailureCategory);
        Assert.Equal(0, invoker.InvocationCount);
    }

    [Fact]
    public async Task AccessDeniedDiagnosticMapsToPermissionFailure()
    {
        var execution = Completed(exitCode: 1, standardError: "Error 5 getting session names");
        var sender = CreateSender(new RecordingProcessInvoker(execution));

        var result = await sender.SendAsync(new MessageRequest("desktop-01", "Test message"));

        Assert.Equal(MessageDeliveryStatus.Rejected, result.Status);
        Assert.Equal(MessageFailureCategory.PermissionDenied, result.FailureCategory);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task RpcDiagnosticMapsToUnavailableTarget()
    {
        var execution = Completed(exitCode: 1, standardError: "Error 1722 getting session names");
        var sender = CreateSender(new RecordingProcessInvoker(execution));

        var result = await sender.SendAsync(new MessageRequest("desktop-01", "Test message"));

        Assert.Equal(MessageDeliveryStatus.Rejected, result.Status);
        Assert.Equal(MessageFailureCategory.TargetUnavailable, result.FailureCategory);
    }

    [Fact]
    public async Task TimeoutMapsToTimedOut()
    {
        var execution = new ProcessExecutionResult(
            ProcessExecutionOutcome.TimedOut,
            -1,
            string.Empty,
            string.Empty,
            TimeSpan.FromMilliseconds(100));
        var sender = CreateSender(new RecordingProcessInvoker(execution));

        var result = await sender.SendAsync(new MessageRequest("desktop-01", "Test message"));

        Assert.Equal(MessageDeliveryStatus.TimedOut, result.Status);
        Assert.Equal(MessageFailureCategory.Timeout, result.FailureCategory);
    }

    [Fact]
    public async Task PreCancelledRequestDoesNotStartProcess()
    {
        var invoker = new RecordingProcessInvoker(Completed(exitCode: 0));
        var sender = CreateSender(invoker);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var result = await sender.SendAsync(
            new MessageRequest("desktop-01", "Test message"),
            cancellationSource.Token);

        Assert.Equal(MessageDeliveryStatus.Cancelled, result.Status);
        Assert.Equal(0, invoker.InvocationCount);
    }

    [Fact]
    public async Task DiagnosticOutputOmitsMessageBody()
    {
        const string message = "private maintenance detail";
        var execution = Completed(exitCode: 1, standardError: $"Rejected: {message}");
        var sender = CreateSender(new RecordingProcessInvoker(execution));

        var result = await sender.SendAsync(new MessageRequest("desktop-01", message));

        Assert.DoesNotContain(message, result.StandardError, StringComparison.Ordinal);
        Assert.Contains("[message omitted]", result.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NumericMessageDoesNotHideNativeErrorClassification()
    {
        const string message = "5";
        var execution = Completed(exitCode: 1, standardError: "Error 5 getting session names");
        var sender = CreateSender(new RecordingProcessInvoker(execution));

        var result = await sender.SendAsync(new MessageRequest("desktop-01", message));

        Assert.Equal(MessageFailureCategory.PermissionDenied, result.FailureCategory);
        Assert.DoesNotContain(message, result.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingExecutableMapsToExecutableUnavailable()
    {
        var execution = new ProcessExecutionResult(
            ProcessExecutionOutcome.StartFailed,
            null,
            string.Empty,
            string.Empty,
            TimeSpan.FromMilliseconds(1),
            NativeErrorCode: 2,
            FailureDiagnostic: "The system cannot find the file specified.");
        var sender = CreateSender(new RecordingProcessInvoker(execution));

        var result = await sender.SendAsync(new MessageRequest("desktop-01", "Test message"));

        Assert.Equal(MessageDeliveryStatus.Failed, result.Status);
        Assert.Equal(MessageFailureCategory.ExecutableUnavailable, result.FailureCategory);
    }

    [Fact]
    public async Task FailedTerminationMapsToProcessTerminationFailure()
    {
        var execution = new ProcessExecutionResult(
            ProcessExecutionOutcome.TimedOut,
            null,
            string.Empty,
            string.Empty,
            TimeSpan.FromSeconds(1),
            TerminationSucceeded: false,
            FailureDiagnostic: "Access denied while terminating process.");
        var sender = CreateSender(new RecordingProcessInvoker(execution));

        var result = await sender.SendAsync(new MessageRequest("desktop-01", "Test message"));

        Assert.Equal(MessageDeliveryStatus.Failed, result.Status);
        Assert.Equal(MessageFailureCategory.ProcessTerminationFailure, result.FailureCategory);
    }

    private static WindowsMsgSender CreateSender(
        IProcessInvoker invoker,
        IWindowsMsgServerResolver? resolver = null)
    {
        return new WindowsMsgSender(
            new WindowsMsgProcessStartInfoFactory(@"C:\Windows\System32\msg.exe"),
            invoker,
            resolver ?? new StubServerResolver(WindowsMsgServerResolution.Success("desktop-01")),
            new WindowsMsgSenderOptions(TimeSpan.FromSeconds(1)));
    }

    private static ProcessExecutionResult Completed(
        int exitCode,
        string standardOutput = "",
        string standardError = "")
    {
        return new ProcessExecutionResult(
            ProcessExecutionOutcome.Completed,
            exitCode,
            standardOutput,
            standardError,
            TimeSpan.FromMilliseconds(10));
    }

    private sealed class RecordingProcessInvoker(ProcessExecutionResult result) : IProcessInvoker
    {
        public int InvocationCount { get; private set; }

        public ProcessStartInfo? LastStartInfo { get; private set; }

        public Task<ProcessExecutionResult> ExecuteAsync(
            ProcessStartInfo startInfo,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            LastStartInfo = startInfo;
            return Task.FromResult(result);
        }
    }

    private sealed class StubServerResolver(WindowsMsgServerResolution resolution)
        : IWindowsMsgServerResolver
    {
        public Task<WindowsMsgServerResolution> ResolveAsync(
            MessageTarget target,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(resolution);
        }
    }

    private sealed class CancellingServerResolver(CancellationTokenSource cancellationSource)
        : IWindowsMsgServerResolver
    {
        public Task<WindowsMsgServerResolution> ResolveAsync(
            MessageTarget target,
            CancellationToken cancellationToken)
        {
            cancellationSource.Cancel();
            return Task.FromCanceled<WindowsMsgServerResolution>(cancellationToken);
        }
    }
}
