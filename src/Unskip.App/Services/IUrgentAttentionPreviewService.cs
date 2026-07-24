namespace Unskip.App.Services;

public interface IUrgentAttentionPreviewService
{
    Task ShowAsync(CancellationToken cancellationToken = default);
}
