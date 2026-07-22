using System.Windows.Input;

namespace Unskip.App.Commands;

public sealed class AsyncRelayCommand(
    Func<object?, Task> execute,
    Predicate<object?>? canExecute = null) : ICommand
{
    private readonly Predicate<object?>? _canExecute = canExecute;
    private readonly Func<object?, Task> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter).ConfigureAwait(true);
    }

    public async Task ExecuteAsync(object? parameter = null)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isExecuting = true;
        NotifyCanExecuteChanged();
        try
        {
            await _execute(parameter).ConfigureAwait(true);
        }
        finally
        {
            _isExecuting = false;
            NotifyCanExecuteChanged();
        }
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
