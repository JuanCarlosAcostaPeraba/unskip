using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Unskip.App.Services;
using Unskip.App.ViewModels;

namespace Unskip.App.Views;

public partial class UrgentAttentionOverlayWindow : Window, IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly UrgentAttentionOverlayViewModel _viewModel;
    private bool _isDisposed;

    internal UrgentAttentionOverlayWindow(
        UrgentAttentionOverlayViewModel viewModel,
        VirtualScreenLayout layout)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = viewModel;
        var bounds = layout.DesktopBounds;
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;
        MessageCard.RenderTransform = new TranslateTransform(
            GetCenter(layout.AttentionBounds.Left, layout.AttentionBounds.Width)
                - GetCenter(bounds.Left, bounds.Width),
            GetCenter(layout.AttentionBounds.Top, layout.AttentionBounds.Height)
                - GetCenter(bounds.Top, bounds.Height));
        if (SystemParameters.HighContrast)
        {
            Background = SystemColors.WindowBrush;
            CloseButton.Foreground = SystemColors.HighlightTextBrush;
        }

        _viewModel.DismissRequested += OnDismissRequested;
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
    }

    private static double GetCenter(double start, double length)
    {
        return start + (length / 2);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        CloseButton.Focus();
        await _viewModel.WaitForTimeoutAsync(_lifetime.Token).ConfigureAwait(true);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _viewModel.DismissRequested -= OnDismissRequested;
        SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
        GC.SuppressFinalize(this);
    }

    private void OnDismissRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SystemParameters.VirtualScreenLeft)
            or nameof(SystemParameters.VirtualScreenTop)
            or nameof(SystemParameters.VirtualScreenWidth)
            or nameof(SystemParameters.VirtualScreenHeight))
        {
            Dispatcher.BeginInvoke(Close);
        }
    }
}
