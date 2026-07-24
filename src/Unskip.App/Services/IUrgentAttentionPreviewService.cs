namespace Unskip.App.Services;

public interface IUrgentAttentionPreviewService
{
    Task ShowAsync(string message, CancellationToken cancellationToken = default);
}
