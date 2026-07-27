using System.Windows;

namespace Unskip.App.Services;

internal sealed class WpfResidentWindow(Window window) : IResidentWindow
{
    private readonly Window _window = window ?? throw new ArgumentNullException(nameof(window));

    public void ShowAndActivate()
    {
        if (!_window.IsVisible)
        {
            _window.Show();
        }

        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }

        _window.Activate();
    }

    public void Hide()
    {
        _window.Hide();
    }
}
