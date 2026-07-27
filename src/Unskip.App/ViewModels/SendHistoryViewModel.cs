using System.Collections.ObjectModel;
using Unskip.App.Commands;
using Unskip.App.Localization;
using Unskip.App.Services;
using Unskip.Core.Messaging;
using Unskip.Core.Messaging.History;

namespace Unskip.App.ViewModels;

public sealed class SendHistoryViewModel : ObservableObject
{
    private readonly List<SendHistoryListItemViewModel> _all = [];
    private readonly IHistoryDeletionConfirmation _confirmation;
    private readonly SendHistoryService _history;
    private string? _searchText;
    private HistoryFilterOption _selectedFilter;
    private SendHistoryListItemViewModel? _selectedEntry;

    public SendHistoryViewModel(SendHistoryService history, IHistoryDeletionConfirmation confirmation)
    {
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _confirmation = confirmation ?? throw new ArgumentNullException(nameof(confirmation));
        Filters =
        [
            new(UiText.Get("HistoryFilterAll"), null),
            new(UiText.Get("DeliverySent"), MessageDeliveryStatus.Sent),
            new(UiText.Get("DeliveryRejected"), MessageDeliveryStatus.Rejected),
            new(UiText.Get("DeliveryTimedOut"), MessageDeliveryStatus.TimedOut),
            new(UiText.Get("DeliveryFailed"), MessageDeliveryStatus.Failed),
        ];
        _selectedFilter = Filters[0];
        RetryCommand = new RelayCommand(_ => Retry(), _ => SelectedEntry is not null);
        DeleteCommand = new AsyncRelayCommand(_ => DeleteAsync(), _ => SelectedEntry is not null);
        ClearCommand = new AsyncRelayCommand(_ => ClearAsync(), _ => _all.Count > 0);
    }

    public event EventHandler<MessagePreparationRequestedEventArgs>? RetryRequested;

    public ObservableCollection<SendHistoryListItemViewModel> FilteredEntries { get; } = [];
    public IReadOnlyList<HistoryFilterOption> Filters { get; }
    public RelayCommand RetryCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public AsyncRelayCommand ClearCommand { get; }

    public string? SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) { ApplyFilter(); } } }
    public HistoryFilterOption SelectedFilter { get => _selectedFilter; set { if (SetProperty(ref _selectedFilter, value)) { ApplyFilter(); } } }
    public SendHistoryListItemViewModel? SelectedEntry { get => _selectedEntry; set { if (SetProperty(ref _selectedEntry, value)) { NotifyCommands(); } } }
    public string CountLabel => _all.Count == 1
        ? UiText.Get("HistoryOneEntry")
        : UiText.Format("HistoryEntryCount", _all.Count);

    public async Task ReloadAsync()
    {
        var records = await _history.GetAllAsync().ConfigureAwait(true);
        _all.Clear();
        _all.AddRange(records.Select(record => new SendHistoryListItemViewModel(record)));
        ApplyFilter();
        OnPropertyChanged(nameof(CountLabel));
        NotifyCommands();
    }

    private void Retry()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        var record = SelectedEntry.Record;
        RetryRequested?.Invoke(this, new MessagePreparationRequestedEventArgs(
            record.AliasSnapshot, record.DestinationSnapshot, record.DestinationKind,
            record.DeviceId, record.ComputerNameSnapshot, record.Ipv4AddressSnapshot));
    }

    private async Task DeleteAsync()
    {
        if (SelectedEntry is null || !await _confirmation.ConfirmDeleteAsync(SelectedEntry.Alias).ConfigureAwait(true))
        {
            return;
        }

        await _history.DeleteAsync(SelectedEntry.Id).ConfigureAwait(true);
        await ReloadAsync().ConfigureAwait(true);
    }

    private async Task ClearAsync()
    {
        if (!await _confirmation.ConfirmClearAsync(_all.Count).ConfigureAwait(true))
        {
            return;
        }

        await _history.ClearAsync().ConfigureAwait(true);
        await ReloadAsync().ConfigureAwait(true);
    }

    private void ApplyFilter()
    {
        var search = SearchText?.Trim();
        var matches = _all.Where(item =>
            (SelectedFilter.Status is null || item.Record.DeliveryStatus == SelectedFilter.Status)
            && (string.IsNullOrWhiteSpace(search)
                || item.Alias.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.Destination.Contains(search, StringComparison.OrdinalIgnoreCase)));
        FilteredEntries.Clear();
        foreach (var item in matches)
        {
            FilteredEntries.Add(item);
        }

        if (SelectedEntry is not null && !FilteredEntries.Contains(SelectedEntry))
        {
            SelectedEntry = null;
        }
    }

    private void NotifyCommands()
    {
        RetryCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        ClearCommand.NotifyCanExecuteChanged();
    }
}
