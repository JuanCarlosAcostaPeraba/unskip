using Unskip.Core.Messaging;

namespace Unskip.Infrastructure.Windows.Tests;

public sealed class NativeMsgIntegrationTests
{
    private const string TargetVariable = "UNSKIP_NATIVE_TEST_TARGET";

    [NativeIntegrationFact]
    [Trait("Category", "NativeIntegration")]
    public async Task ExplicitTargetCanReceiveNativeTestMessage()
    {
        var target = Environment.GetEnvironmentVariable(TargetVariable);
        Assert.False(
            string.IsNullOrWhiteSpace(target),
            $"Set {TargetVariable} to a computer name or IPv4 address that you are authorized to test.");

        var executablePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "System32",
            "msg.exe");
        var sender = new WindowsMsgSender(
            new WindowsMsgProcessStartInfoFactory(executablePath),
            new SystemProcessInvoker(),
            new WindowsMsgServerResolver(),
            new WindowsMsgSenderOptions(TimeSpan.FromSeconds(10)));

        var result = await sender.SendAsync(
            new MessageRequest(target, "Unskip opt-in native integration test."));

        Assert.Equal(MessageDeliveryStatus.Sent, result.Status);
    }
}
