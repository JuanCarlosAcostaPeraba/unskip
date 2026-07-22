namespace Unskip.App.Services;

public interface IHistoryDeletionConfirmation
{
    Task<bool> ConfirmDeleteAsync(string destinationAlias);

    Task<bool> ConfirmClearAsync(int count);
}
