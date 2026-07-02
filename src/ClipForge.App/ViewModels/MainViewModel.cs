using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using ClipForge.App.Clipboard;
using ClipForge.App.Services;
using ClipForge.App.Storage;
using ClipForge.Core.Models;
using ClipForge.Core.Services;
using ClipForge.Core.Settings;
using ClipForge.Core.Storage;
using WpfClipboard = System.Windows.Clipboard;

namespace ClipForge.App.ViewModels;

/// <summary>A selectable list ordering shown in the "Newest first" dropdown.</summary>
public sealed record SortOption(string Label, ClipSort Sort)
{
    public override string ToString() => Label;
}

/// <summary>
/// Drives the main window: live list of entries, search, sidebar filters with
/// counts, sort order, and per-item actions. Listens to the capture coordinator
/// so newly copied clips appear immediately.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IClipRepository _repository;
    private readonly ClipboardCaptureCoordinator _coordinator;
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogs;
    private readonly ImageStore _imageStore;

    public ObservableCollection<ClipEntry> Entries { get; } = new();

    public IReadOnlyList<FilterItem> Filters { get; }

    public IReadOnlyList<SortOption> SortOptions { get; } = new[]
    {
        new SortOption("Newest first", ClipSort.NewestFirst),
        new SortOption("Oldest first", ClipSort.OldestFirst),
    };

    [ObservableProperty]
    private FilterItem _selectedFilter;

    [ObservableProperty]
    private SortOption _selectedSort;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ClipEntry? _selectedEntry;

    [ObservableProperty]
    private bool _isPaused;

    /// <summary>Number of entries currently shown (drives the "N clips" header).</summary>
    [ObservableProperty]
    private int _visibleCount;

    /// <summary>How many entries are selected (for the multiselect toolbar).</summary>
    [ObservableProperty]
    private int _selectedCount;

    /// <summary>Inclusive start of the date filter (day granularity); null = unbounded.</summary>
    [ObservableProperty]
    private DateTime? _fromDate;

    /// <summary>Inclusive end of the date filter (day granularity); null = unbounded.</summary>
    [ObservableProperty]
    private DateTime? _toDate;

    /// <summary>Raised when the user asks to open Settings (handled by the app shell).</summary>
    public event EventHandler? SettingsRequested;

    public MainViewModel(
        IClipRepository repository,
        ClipboardCaptureCoordinator coordinator,
        ISettingsService settings,
        IDialogService dialogs,
        ImageStore imageStore)
    {
        _repository = repository;
        _coordinator = coordinator;
        _settings = settings;
        _dialogs = dialogs;
        _imageStore = imageStore;

        Filters = ClipFilter.Sidebar.Select(f => new FilterItem(f)).ToList();
        _selectedFilter = Filters[0];
        _selectedSort = SortOptions[0];

        _coordinator.EntryStored += OnEntryStored;
    }

    partial void OnSearchTextChanged(string value) => Refresh();

    partial void OnSelectedFilterChanged(FilterItem value) => Refresh();

    partial void OnSelectedSortChanged(SortOption value) => Refresh();

    partial void OnFromDateChanged(DateTime? value) => Refresh();

    partial void OnToDateChanged(DateTime? value) => Refresh();

    partial void OnIsPausedChanged(bool value) => _coordinator.IsPaused = value;

    [RelayCommand]
    private void ClearDateFilter()
    {
        FromDate = null;
        ToDate = null;
    }

    [RelayCommand]
    public void Refresh()
    {
        var filter = SelectedFilter?.Filter ?? ClipFilter.Sidebar[0];
        var query = new ClipQuery
        {
            Search = SearchText,
            Type = filter.Type,
            FavoritesOnly = filter.FavoritesOnly,
            // FromDate/ToDate are picked as local calendar days; widen to cover
            // the full day (00:00 .. 23:59:59.999) so the bound is inclusive.
            From = FromDate is { } f ? new DateTimeOffset(f.Date) : null,
            To = ToDate is { } t ? new DateTimeOffset(t.Date.AddDays(1).AddTicks(-1)) : null,
            Sort = SelectedSort?.Sort ?? ClipSort.NewestFirst,
            Limit = 200
        };
        var results = _repository.Query(query);

        Entries.Clear();
        foreach (var entry in results)
            Entries.Add(entry);
        VisibleCount = Entries.Count;

        UpdateCounts();
    }

    private void UpdateCounts()
    {
        var counts = _repository.GetCounts();
        foreach (var item in Filters)
            item.UpdateCount(counts);
    }

    [RelayCommand]
    private void OpenSettings() => SettingsRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Copy(ClipEntry? entry)
    {
        entry ??= SelectedEntry;
        if (entry is null) return;

        if (entry.Type == ClipType.Image)
        {
            CopyImageToClipboard(entry);
            return;
        }

        if (entry.Content is not { } content) return;
        _coordinator.SuppressNextChange();
        WpfClipboard.SetText(content);
    }

    private void CopyImageToClipboard(ClipEntry entry)
    {
        var image = _repository.GetImage(entry.Id);
        if (image is null || !File.Exists(image.FilePath)) return;

        // Rich multi-format data object (file + PNG + bitmap) so the image pastes
        // reliably everywhere, with retries in case the clipboard is momentarily locked.
        var data = ClipboardImageHelper.BuildImageData(new[] { image.FilePath });
        _coordinator.SuppressNextChange();
        ClipboardImageHelper.TrySetClipboard(data);
    }

    /// <summary>Open an image clip in the system default viewer.</summary>
    [RelayCommand]
    private void Open(ClipEntry? entry)
    {
        entry ??= SelectedEntry;
        if (entry is null || entry.Type != ClipType.Image) return;
        if (_repository.GetImage(entry.Id) is { } image && File.Exists(image.FilePath))
            Process.Start(new ProcessStartInfo(image.FilePath) { UseShellExecute = true });
    }

    /// <summary>Delete a set of entries (multiselect), confirming once for the batch.</summary>
    public void DeleteEntries(IReadOnlyList<ClipEntry> entries)
    {
        if (entries.Count == 0) return;

        if (_settings.Current.ConfirmBeforeDelete &&
            !_dialogs.Confirm($"Delete {entries.Count} clip(s)?", "ClipKeep"))
            return;

        foreach (var entry in entries.ToList())
        {
            if (entry.Type == ClipType.Image && _repository.GetImage(entry.Id) is { } image)
                _imageStore.DeleteFiles(image);
            _repository.Delete(entry.Id);
            Entries.Remove(entry);
        }
        VisibleCount = Entries.Count;
        UpdateCounts();
    }

    /// <summary>
    /// Build a drag payload for the given entries: image files (file drop) when any
    /// are images, otherwise the joined text. Null if there's nothing draggable.
    /// </summary>
    public DataObject? BuildDragData(IReadOnlyList<ClipEntry> entries)
    {
        var imagePaths = new List<string>();
        var texts = new List<string>();
        foreach (var entry in entries)
        {
            if (entry.Type == ClipType.Image && _repository.GetImage(entry.Id) is { } image &&
                File.Exists(image.FilePath))
                imagePaths.Add(image.FilePath);
            else if (entry.Content is { } content)
                texts.Add(content);
        }

        if (imagePaths.Count > 0)
            return ClipboardImageHelper.BuildImageData(imagePaths);
        if (texts.Count > 0)
        {
            var data = new DataObject();
            data.SetText(string.Join(Environment.NewLine, texts));
            return data;
        }
        return null;
    }

    [RelayCommand]
    private void ToggleFavorite(ClipEntry? entry)
    {
        entry ??= SelectedEntry;
        if (entry is null) return;

        var newValue = !entry.Favorite;
        _repository.SetFavorite(entry.Id, newValue);
        entry.Favorite = newValue;
        Refresh();
    }

    [RelayCommand]
    private void Delete(ClipEntry? entry)
    {
        entry ??= SelectedEntry;
        if (entry is null) return;

        if (_settings.Current.ConfirmBeforeDelete &&
            !_dialogs.Confirm("Delete this clipboard entry?", "ClipKeep"))
            return;

        // Remove image files before the row (cascade drops the images record).
        if (entry.Type == ClipType.Image && _repository.GetImage(entry.Id) is { } image)
            _imageStore.DeleteFiles(image);

        _repository.Delete(entry.Id);
        Entries.Remove(entry);
        VisibleCount = Entries.Count;
        UpdateCounts();
    }

    private void OnEntryStored(object? sender, StoreResult e)
    {
        // Raised off the UI thread by the capture pipeline; marshal back.
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                Refresh();
        });
    }

    public void Dispose() => _coordinator.EntryStored -= OnEntryStored;
}
