using System;
using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace DevWinUI;

/// <summary>
/// Coordinates fullscreen preview popup opening, instant strip follow sync, and connected
/// animations. Ported faithfully from Microsoft Store PDP's <c>PreviewViewerHelper</c>.
/// </summary>
public class PreviewViewerHelper
{
    private PreviewViewer _previewViewer;                      // [0x20]
    private PreviewPopupHost _popup;                           // [0x28]
    private Action<object, string> _warningAction;             // [0x30]
    private PreviewViewerHelperListViewSource _listViewSource; // [0x38]
    private int _mode;                                         // [0x40]

    public PreviewViewer PreviewViewer
    {
        get => _previewViewer;
        set => _previewViewer = value;
    }

    public PreviewPopupHost Popup
    {
        get => _popup;
        set
        {
            if (_popup?.List != null)
            {
                _popup.List.PropertyChanged -= OnPopupPreviewSelectionChanged;
            }
            _popup = value;
            if (_popup?.List != null)
            {
                _popup.List.PropertyChanged += OnPopupPreviewSelectionChanged;
            }
        }
    }

    public PreviewViewerHelperListViewSource ListViewSource
    {
        get => _listViewSource;
        set => _listViewSource = value;
    }

    public int Mode
    {
        get => _mode;
        set => _mode = value;
    }

    public Action<object, string> WarningAction
    {
        get => _warningAction;
        set => _warningAction = value;
    }

    /// <summary>
    /// Opens the preview viewer for the specified item and initiates forward connected animation.
    /// Recovered from <c>0x18209E190</c>.
    /// </summary>
    public void SetUpPreviewViewer(object dataItem, bool flag = false)
    {
        var popup = _popup;
        if (popup == null || popup.List == null)
        {
            return;
        }

        if (!IsValidDataItem(dataItem, flag))
        {
            _warningAction?.Invoke(dataItem, nameof(SetUpPreviewViewer));
            return;
        }

        if (_listViewSource != null)
        {
            _listViewSource.PrepareForwardConnectedAnimation(dataItem);
        }

        popup.List.CurrentItem = dataItem;

        int index = popup.List.IndexOf(dataItem);
        OpenPreviewViewer(index, dataItem);
    }

    /// <summary>
    /// Instant strip synchronization: responds to <c>PropertyChanged</c> for "SelectedIndex"
    /// and smoothly scrolls the underlying strip to match. Recovered from <c>0x18209E5D0</c>.
    /// </summary>
    public void OnPopupPreviewSelectionChanged(object sender, PropertyChangedEventArgs e)
    {
        var popup = _popup;
        if (popup == null || popup.List == null || popup.Items == null)
        {
            return;
        }

        if (!string.Equals(e?.PropertyName, "SelectedIndex", StringComparison.Ordinal))
        {
            return;
        }

        object selected = popup.List.SelectedItem ?? popup.List.CurrentItem;
        int index = selected != null ? popup.Items.IndexOf(selected) : -1;

        if (index < 0)
        {
            if (popup.List.SelectedIndex < 0)
            {
                return;
            }

            int current = popup.List.SelectedIndex;
            if (popup.Items.Count <= current)
            {
                return;
            }

            index = popup.List.SelectedIndex;
        }

        if (_listViewSource != null)
        {
            _listViewSource.ScrollToIndex(index);
        }
    }

    /// <summary>
    /// Opens the overlay preview viewer and sets the selection index.
    /// Recovered from <c>0x18209E820</c> (Hex-Rays):
    /// clamp index → <c>popup.List.SelectedIndex = index</c> (<c>0x1823F0170</c>) →
    /// <c>OpenOverlayPopup</c> (<c>0x182172020</c>). Does not rebind items or set FlipView directly.
    /// </summary>
    public void OpenPreviewViewer(int index, object dataItem)
    {
        if (index < 0)
        {
            return;
        }

        var popup = _popup;
        if (popup == null || popup.List == null)
        {
            return;
        }

        // [ASM] count from list items collection at [list+0x20] (same collection IndexOf searched).
        int count = popup.List.GetItemCount();
        if (count <= 0)
        {
            return;
        }

        // [ASM] cmovle ebx, 0 when count <= index
        if (count <= index)
        {
            index = 0;
        }

        // 0x1823F0170 only — PreviewViewer FlipView updates via PropertyChanged (0x18225A1B0).
        popup.List.SelectedIndex = index;

        if (_previewViewer == null)
        {
            return;
        }

        // 0x182172020 -> sub_180D8FDF0 Focus(FlipView, Programmatic)
        _previewViewer.OpenOverlayPopup();
    }

    /// <summary>
    /// Validates data item compatibility. Recovered from <c>0x18209E3E0</c>.
    /// </summary>
    public bool IsValidDataItem(object dataItem, bool flag)
    {
        if (dataItem == null)
        {
            return false;
        }

        if (dataItem is ScreenshotTileItem)
        {
            return _mode == 0;
        }

        if (dataItem is VideoPlayerSource)
        {
            if (_mode == 0)
            {
                return true;
            }

            return _mode == 1 && flag;
        }

        return true;
    }

    /// <summary>
    /// Extracts item key identifier string. Recovered from <c>0x18209E510</c>.
    /// </summary>
    public string GetItemKey(object dataItem)
    {
        if (dataItem == null)
        {
            return string.Empty;
        }

        if (dataItem is ScreenshotTileItem screenshot)
        {
            return screenshot.ImageUri?.ToString() ?? screenshot.Title ?? string.Empty;
        }

        if (dataItem is VideoPlayerSource video)
        {
            return video.Source?.ToString() ?? video.ImageUri?.ToString() ?? video.Title ?? string.Empty;
        }

        return dataItem.ToString() ?? string.Empty;
    }
}

public class PreviewPopupHost
{
    public PreviewListFacade List { get; set; }
    public System.Collections.Generic.IList<object> Items { get; set; }
    public PreviewPopupContent Content { get; set; }
}

public class PreviewPopupContent
{
    public Uri Uri { get; set; }
}

public class PreviewListFacade : INotifyPropertyChanged
{
    private object _currentItem;
    private object _selectedItem;
    private int _selectedIndex = -1;
    private PreviewItemsViewModel _boundViewModel;
    private bool _syncingFromBound;

    public event PropertyChangedEventHandler PropertyChanged;

    public object CurrentItem
    {
        get => _currentItem;
        set
        {
            if (_currentItem != value)
            {
                _currentItem = value;
                OnPropertyChanged(nameof(CurrentItem));
                if (!_syncingFromBound && _boundViewModel != null && !ReferenceEquals(_boundViewModel.CurrentItem, value))
                {
                    _boundViewModel.CurrentItem = value;
                }
            }
        }
    }

    public object SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value)
            {
                _selectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
                OnPropertyChanged("Caption");
                OnPropertyChanged("CurrentIndex");
                OnPropertyChanged("TotalIndex");
                if (!_syncingFromBound && _boundViewModel != null && _boundViewModel.SelectedIndex != value)
                {
                    _boundViewModel.SelectedIndex = value;
                }
            }
        }
    }

    public ItemCollection Items { get; set; }

    /// <summary>
    /// Faithful to <c>0x1823F0A40</c>: search preview list VM items (<c>[list+0x20]</c>); return −1 when not found.
    /// </summary>
    public int IndexOf(object item)
    {
        if (item == null)
        {
            return -1;
        }

        // Prefer bound list VM items — same collection FlipView binds to (Store IndexOf target).
        if (_boundViewModel?.Items != null)
        {
            return _boundViewModel.Items.IndexOf(item);
        }

        if (Items != null)
        {
            return Items.IndexOf(item);
        }

        return -1;
    }

    /// <summary>
    /// Item count from the list VM collection used by <c>OpenPreviewViewer</c> clamp (<c>[list+0x20]+count</c>).
    /// </summary>
    public int GetItemCount()
    {
        if (_boundViewModel?.Items != null)
        {
            return _boundViewModel.Items.Count;
        }

        if (Items != null)
        {
            return Items.Count;
        }

        return 0;
    }

    /// <summary>
    /// Binds this facade to the same <see cref="PreviewItemsViewModel"/> instance used by
    /// <see cref="PreviewViewer"/> so SelectedIndex PropertyChanged reaches FlipView (Store contract).
    /// </summary>
    public void BindToViewModel(PreviewItemsViewModel viewModel)
    {
        if (_boundViewModel != null)
        {
            _boundViewModel.PropertyChanged -= OnBoundViewModelPropertyChanged;
        }

        _boundViewModel = viewModel;
        if (_boundViewModel != null)
        {
            _boundViewModel.PropertyChanged += OnBoundViewModelPropertyChanged;
            SyncFromBoundViewModel();
        }
    }

    private void OnBoundViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e?.PropertyName)
            || string.Equals(e.PropertyName, "SelectedIndex", StringComparison.Ordinal)
            || string.Equals(e.PropertyName, "CurrentItem", StringComparison.Ordinal))
        {
            SyncFromBoundViewModel();
        }
    }

    private void SyncFromBoundViewModel()
    {
        if (_boundViewModel == null)
        {
            return;
        }

        _syncingFromBound = true;
        try
        {
            CurrentItem = _boundViewModel.CurrentItem;
            SelectedItem = _boundViewModel.CurrentItem;
            SelectedIndex = _boundViewModel.SelectedIndex;
        }
        finally
        {
            _syncingFromBound = false;
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
