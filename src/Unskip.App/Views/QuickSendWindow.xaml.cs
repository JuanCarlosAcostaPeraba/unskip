using System.Windows;
using Unskip.App.ViewModels;

namespace Unskip.App.Views;

public partial class QuickSendWindow : Window
{
    public QuickSendWindow(QuickSendViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }
}
