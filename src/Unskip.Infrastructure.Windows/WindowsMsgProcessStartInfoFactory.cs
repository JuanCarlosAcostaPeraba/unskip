using System.Diagnostics;
using Unskip.Core.Messaging;

namespace Unskip.Infrastructure.Windows;

internal sealed class WindowsMsgProcessStartInfoFactory
{
    private readonly string _executablePath;

    public WindowsMsgProcessStartInfoFactory()
        : this(GetSystemMsgExecutablePath())
    {
    }

    internal WindowsMsgProcessStartInfoFactory(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        _executablePath = Path.GetFullPath(executablePath);
    }

    public ProcessStartInfo Create(ValidatedMessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Target.Kind is not MessageTargetKind.Hostname and not MessageTargetKind.Ipv4Address)
        {
            throw new NotSupportedException("Windows msg.exe delivery requires a validated hostname or canonical IPv4 address.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            ErrorDialog = false,
        };

        startInfo.ArgumentList.Add("*");
        startInfo.ArgumentList.Add($"/SERVER:{request.Target.Value}");
        startInfo.ArgumentList.Add(request.Message);

        return startInfo;
    }

    private static string GetSystemMsgExecutablePath()
    {
        var systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
        if (string.IsNullOrWhiteSpace(systemDirectory))
        {
            throw new PlatformNotSupportedException("The Windows system directory could not be resolved.");
        }

        return Path.Combine(systemDirectory, "msg.exe");
    }
}
