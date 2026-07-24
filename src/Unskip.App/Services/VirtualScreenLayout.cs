namespace Unskip.App.Services;

internal readonly record struct VirtualScreenLayout
{
    public VirtualScreenLayout(
        VirtualScreenBounds desktopBounds,
        VirtualScreenBounds attentionBounds)
    {
        if (attentionBounds.Left < desktopBounds.Left
            || attentionBounds.Top < desktopBounds.Top
            || attentionBounds.Left + attentionBounds.Width > desktopBounds.Left + desktopBounds.Width
            || attentionBounds.Top + attentionBounds.Height > desktopBounds.Top + desktopBounds.Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attentionBounds),
                "The attention area must be contained within the virtual desktop.");
        }

        DesktopBounds = desktopBounds;
        AttentionBounds = attentionBounds;
    }

    public VirtualScreenBounds DesktopBounds { get; }

    public VirtualScreenBounds AttentionBounds { get; }
}
