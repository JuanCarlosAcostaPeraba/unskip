using Unskip.Core.Messaging;

namespace Unskip.Infrastructure.Windows.Tests;

public sealed class WindowsMsgProcessStartInfoFactoryTests
{
    [Fact]
    public void DefaultFactoryResolvesSystem32MsgExecutable()
    {
        var request = new ValidatedMessageRequest(
            new MessageTarget("desktop-01", MessageTargetKind.Hostname),
            "Test message");
        var factory = new WindowsMsgProcessStartInfoFactory();

        var startInfo = factory.Create(request);

        Assert.True(File.Exists(startInfo.FileName));
        Assert.Equal("msg.exe", Path.GetFileName(startInfo.FileName));
        Assert.Equal(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            Path.GetDirectoryName(startInfo.FileName));
    }

    [Fact]
    public void CreateUsesDirectExecutableAndIndependentArguments()
    {
        const string executablePath = @"C:\Windows\System32\msg.exe";
        const string message = "Quoted \"text\" & | < > ^ %PATH% $(ignored)";
        var request = new ValidatedMessageRequest(
            new MessageTarget("desktop-01", MessageTargetKind.Hostname),
            message);
        var factory = new WindowsMsgProcessStartInfoFactory(executablePath);

        var startInfo = factory.Create(request);

        Assert.Equal(executablePath, startInfo.FileName);
        Assert.False(startInfo.UseShellExecute);
        Assert.True(startInfo.CreateNoWindow);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
        Assert.Equal(["*", "/SERVER:desktop-01", message], startInfo.ArgumentList);
        Assert.Empty(startInfo.Arguments);
    }

    [Fact]
    public void CreateRejectsNonHostnameTarget()
    {
        var request = new ValidatedMessageRequest(
            new MessageTarget("192.0.2.25", MessageTargetKind.Ipv4Address),
            "Test message");
        var factory = new WindowsMsgProcessStartInfoFactory(@"C:\Windows\System32\msg.exe");

        Assert.Throws<NotSupportedException>(() => factory.Create(request));
    }
}
