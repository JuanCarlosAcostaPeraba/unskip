using Unskip.Core.Messaging;

namespace Unskip.Infrastructure.Windows;

public sealed class WindowsMsgSender : IMessageSender
{
    private readonly WindowsMsgProcessStartInfoFactory _startInfoFactory;
    private readonly IProcessInvoker _processInvoker;
    private readonly WindowsMsgSenderOptions _options;

    public WindowsMsgSender(WindowsMsgSenderOptions? options = null)
        : this(
            new WindowsMsgProcessStartInfoFactory(),
            new SystemProcessInvoker(),
            options ?? new WindowsMsgSenderOptions())
    {
    }

    internal WindowsMsgSender(
        WindowsMsgProcessStartInfoFactory startInfoFactory,
        IProcessInvoker processInvoker,
        WindowsMsgSenderOptions options)
    {
        _startInfoFactory = startInfoFactory;
        _processInvoker = processInvoker;
        _options = options;
    }

    public async Task<MessageSendResult> SendAsync(
        MessageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = MessageRequestValidator.Validate(request);
        if (!validation.IsValid)
        {
            return new MessageSendResult(
                MessageDeliveryStatus.Rejected,
                MessageFailureCategory.Validation,
                null,
                string.Empty,
                string.Empty,
                TimeSpan.Zero,
                validation.Errors[0].Message);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new MessageSendResult(
                MessageDeliveryStatus.Cancelled,
                MessageFailureCategory.Cancelled,
                null,
                string.Empty,
                string.Empty,
                TimeSpan.Zero,
                "Sending was cancelled before Windows was contacted.");
        }

        var startInfo = _startInfoFactory.Create(validation.Request!);
        var execution = await _processInvoker.ExecuteAsync(
            startInfo,
            _options.Timeout,
            cancellationToken).ConfigureAwait(false);

        var standardOutput = DiagnosticSanitizer.Sanitize(execution.StandardOutput, request.Message);
        var standardError = DiagnosticSanitizer.Sanitize(execution.StandardError, request.Message);

        if (!execution.TerminationSucceeded)
        {
            return CreateResult(
                execution,
                MessageDeliveryStatus.Failed,
                MessageFailureCategory.ProcessTerminationFailure,
                standardOutput,
                DiagnosticSanitizer.Sanitize(execution.FailureDiagnostic, request.Message),
                "Unskip could not safely stop the Windows messaging process.");
        }

        return execution.Outcome switch
        {
            ProcessExecutionOutcome.Completed when execution.ExitCode == 0 =>
                CreateResult(
                    execution,
                    MessageDeliveryStatus.Sent,
                    MessageFailureCategory.None,
                    standardOutput,
                    standardError,
                    "Windows accepted the message request. This does not confirm that a person read it."),
            ProcessExecutionOutcome.Completed => CreateNativeFailureResult(execution, standardOutput, standardError),
            ProcessExecutionOutcome.TimedOut =>
                CreateResult(
                    execution,
                    MessageDeliveryStatus.TimedOut,
                    MessageFailureCategory.Timeout,
                    standardOutput,
                    standardError,
                    "Windows did not finish the message request before the timeout."),
            ProcessExecutionOutcome.Cancelled =>
                CreateResult(
                    execution,
                    MessageDeliveryStatus.Cancelled,
                    MessageFailureCategory.Cancelled,
                    standardOutput,
                    standardError,
                    "Sending was cancelled."),
            ProcessExecutionOutcome.StartFailed => CreateStartFailureResult(execution, request.Message),
            _ => CreateResult(
                execution,
                MessageDeliveryStatus.Failed,
                MessageFailureCategory.ProcessFailure,
                standardOutput,
                DiagnosticSanitizer.Sanitize(execution.FailureDiagnostic, request.Message),
                "The Windows messaging process failed unexpectedly."),
        };
    }

    private static MessageSendResult CreateNativeFailureResult(
        ProcessExecutionResult execution,
        string standardOutput,
        string standardError)
    {
        var mapping = NativeFailureMapper.Map(
            execution.ExitCode!.Value,
            execution.StandardOutput,
            execution.StandardError);
        return CreateResult(
            execution,
            MessageDeliveryStatus.Rejected,
            mapping.Category,
            standardOutput,
            standardError,
            mapping.UserMessage);
    }

    private static MessageSendResult CreateStartFailureResult(
        ProcessExecutionResult execution,
        string messageBody)
    {
        var executableUnavailable = execution.NativeErrorCode is 2 or 3;
        return CreateResult(
            execution,
            MessageDeliveryStatus.Failed,
            executableUnavailable
                ? MessageFailureCategory.ExecutableUnavailable
                : MessageFailureCategory.ProcessFailure,
            string.Empty,
            DiagnosticSanitizer.Sanitize(execution.FailureDiagnostic, messageBody),
            executableUnavailable
                ? "Windows msg.exe is not available on this computer."
                : "Windows could not start the native messaging process.");
    }

    private static MessageSendResult CreateResult(
        ProcessExecutionResult execution,
        MessageDeliveryStatus status,
        MessageFailureCategory failureCategory,
        string standardOutput,
        string standardError,
        string userMessage)
    {
        return new MessageSendResult(
            status,
            failureCategory,
            execution.ExitCode,
            standardOutput,
            standardError,
            execution.Duration,
            userMessage);
    }
}
