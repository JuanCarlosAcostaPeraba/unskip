namespace Unskip.Core.Updates;

public interface IUpdateInstallerLauncher
{
    bool TryLaunch(string installerPath);
}
