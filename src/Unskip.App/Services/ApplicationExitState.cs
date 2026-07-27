namespace Unskip.App.Services;

internal sealed class ApplicationExitState
{
    public bool IsExitRequested { get; private set; }

    public void RequestExit()
    {
        IsExitRequested = true;
    }
}
