using Unskip.Infrastructure.Windows;

namespace Unskip.Infrastructure.Windows.Tests;

public sealed class UpdateInstallerStartInfoFactoryTests
{
    [Fact]
    public void InstallerIsStartedDirectlyWithoutShellOrArguments()
    {
        var installerPath = Path.Combine(
            Path.GetTempPath(),
            "Unskip.Tests",
            "Unskip-0.2.0-win-x64-setup.exe");

        var startInfo = UpdateInstallerStartInfoFactory.Create(installerPath);

        Assert.Equal(Path.GetFullPath(installerPath), startInfo.FileName);
        Assert.Equal(Path.GetDirectoryName(Path.GetFullPath(installerPath)), startInfo.WorkingDirectory);
        Assert.False(startInfo.UseShellExecute);
        Assert.False(startInfo.CreateNoWindow);
        Assert.Empty(startInfo.ArgumentList);
    }

    [Fact]
    public void NonExecutableInstallerIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => UpdateInstallerStartInfoFactory.Create(@"C:\updates\candidate.zip"));
    }
}
