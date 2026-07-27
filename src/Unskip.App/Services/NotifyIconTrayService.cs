using System.Drawing;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;

namespace Unskip.App.Services;

internal sealed class NotifyIconTrayService : ITrayService
{
    private readonly Stream _iconStream;
    private readonly Icon _icon;
    private readonly Forms.NotifyIcon _notifyIcon;
    private bool _isDisposed;

    public NotifyIconTrayService()
    {
        var resource = Application.GetResourceStream(
            new Uri("pack://application:,,,/Assets/unskip.ico"))
            ?? throw new InvalidOperationException("The packaged Unskip icon could not be loaded.");
        _iconStream = resource.Stream;
        _icon = new Icon(_iconStream);

        var openItem = new Forms.ToolStripMenuItem(Localization.UiText.Get("TrayOpen"));
        openItem.Click += (_, _) => OpenMainRequested?.Invoke(this, EventArgs.Empty);
        var quickSendItem = new Forms.ToolStripMenuItem(Localization.UiText.Get("TrayQuickSend"));
        quickSendItem.Click += (_, _) => QuickSendRequested?.Invoke(this, EventArgs.Empty);
        var exitItem = new Forms.ToolStripMenuItem(Localization.UiText.Get("TrayExit"));
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(quickSendItem);
        menu.Items.Add(openItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = _icon,
            Text = Localization.UiText.Get("TrayTooltip"),
            Visible = true,
        };
        _notifyIcon.MouseClick += OnMouseClick;
    }

    public event EventHandler? OpenMainRequested;

    public event EventHandler? QuickSendRequested;

    public event EventHandler? ExitRequested;

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _notifyIcon.MouseClick -= OnMouseClick;
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _icon.Dispose();
        _iconStream.Dispose();
    }

    private void OnMouseClick(object? sender, Forms.MouseEventArgs eventArgs)
    {
        if (eventArgs.Button == Forms.MouseButtons.Left)
        {
            QuickSendRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
