using System.Diagnostics;

namespace Unskip.Infrastructure.Windows.Tests;

public sealed class SystemProcessInvokerTests
{
    [Fact]
    public async Task ExecuteAsyncCapturesOutputAndExitCode()
    {
        var startInfo = CreateHelperStartInfo("emit", "standard output", "standard error", "23");
        var invoker = new SystemProcessInvoker();

        var result = await invoker.ExecuteAsync(startInfo, TimeSpan.FromSeconds(5), CancellationToken.None);

        Assert.Equal(ProcessExecutionOutcome.Completed, result.Outcome);
        Assert.Equal(23, result.ExitCode);
        Assert.Equal("standard output", result.StandardOutput);
        Assert.Equal("standard error", result.StandardError);
    }

    [Fact]
    public async Task ExecuteAsyncTerminatesProcessAfterTimeout()
    {
        var startInfo = CreateHelperStartInfo("delay", "30000");
        var invoker = new SystemProcessInvoker();

        var result = await invoker.ExecuteAsync(startInfo, TimeSpan.FromMilliseconds(150), CancellationToken.None);

        Assert.Equal(ProcessExecutionOutcome.TimedOut, result.Outcome);
        Assert.True(result.TerminationSucceeded);
        Assert.True(result.Duration < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExecuteAsyncTerminatesProcessAfterCancellation()
    {
        var startInfo = CreateHelperStartInfo("delay", "30000");
        var invoker = new SystemProcessInvoker();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        var result = await invoker.ExecuteAsync(startInfo, TimeSpan.FromSeconds(10), cancellationSource.Token);

        Assert.Equal(ProcessExecutionOutcome.Cancelled, result.Outcome);
        Assert.True(result.TerminationSucceeded);
        Assert.True(result.Duration < TimeSpan.FromSeconds(5));
    }

    private static ProcessStartInfo CreateHelperStartInfo(params string[] arguments)
    {
        var executablePath = Path.Combine(AppContext.BaseDirectory, "Unskip.TestProcess.exe");
        Assert.True(File.Exists(executablePath), $"Test process was not found at {executablePath}.");

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
