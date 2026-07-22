namespace Unskip.App.Services;

public interface IDeviceDeletionConfirmation
{
    Task<bool> ConfirmAsync(string deviceAlias);
}
