namespace Unskip.Core.Links;

public interface IExternalUriLauncher
{
    bool TryOpen(Uri uri);
}
