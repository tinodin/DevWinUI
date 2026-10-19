using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevWinUI;

public partial class PreviewViewerHelper : ObservableObject
{
    public PreviewViewer PreviewViewer { get; set; }

    [ObservableProperty]
    public partial PreviewPopupHost Popup { get; set; }

    partial void OnPopupChanging(PreviewPopupHost oldValue, PreviewPopupHost newValue)
    {
        if (oldValue?.List != null)
        {
            oldValue.List.PropertyChanged -= OnPopupPreviewSelectionChanged;
        }
    }

    partial void OnPopupChanged(PreviewPopupHost oldValue, PreviewPopupHost newValue)
    {
        if (newValue?.List != null)
        {
            newValue.List.PropertyChanged += OnPopupPreviewSelectionChanged;
        }
    }

    public PreviewViewerHelperListViewSource ListViewSource { get; set; }

    [ObservableProperty]
    public partial PreviewType PreviewMode { get; set; }

    public void SetUpPreviewViewer(object dataItem)
    {
        var popup = Popup;
        if (popup == null || popup.List == null || dataItem == null)
        {
            return;
        }

        ListViewSource?.PrepareForwardConnectedAnimation(dataItem);

        popup.List.CurrentItem = dataItem;

        int index = popup.List.IndexOf(dataItem);
        OpenPreviewViewer(index, dataItem);
    }

    public void OnPopupPreviewSelectionChanged(object sender, PropertyChangedEventArgs e)
    {
        var popup = Popup;
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

        ListViewSource?.ScrollToIndex(index);
    }

    public void OpenPreviewViewer(int index, object dataItem)
    {
        if (index < 0)
        {
            return;
        }

        var popup = Popup;
        if (popup == null || popup.List == null)
        {
            return;
        }

        int count = popup.List.GetItemCount();
        if (count <= 0)
        {
            return;
        }

        if (count <= index)
        {
            index = 0;
        }

        popup.List.SelectedIndex = index;

        if (PreviewViewer == null)
        {
            return;
        }

        PreviewViewer.OpenOverlayPopup();
    }

}

public class PreviewPopupHost
{
    public PreviewListFacade List { get; set; }
    public IList<object> Items { get; set; }
}

public partial class PreviewListFacade : ObservableObject
{
    private PreviewItemsViewModel _boundViewModel;
    private bool _syncingFromBound;

    public PreviewListFacade()
    {
        SelectedIndex = -1;
    }

    [ObservableProperty]
    public partial object CurrentItem { get; set; }

    partial void OnCurrentItemChanged(object oldValue, object newValue)
    {
        if (!_syncingFromBound && _boundViewModel != null && !ReferenceEquals(_boundViewModel.CurrentItem, newValue))
        {
            _boundViewModel.CurrentItem = newValue;
        }
    }

    [ObservableProperty]
    public partial object SelectedItem { get; set; }

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    partial void OnSelectedIndexChanged(int oldValue, int newValue)
    {
        OnPropertyChanged("Caption");
        OnPropertyChanged("CurrentIndex");
        OnPropertyChanged("TotalIndex");

        if (!_syncingFromBound && _boundViewModel != null && _boundViewModel.SelectedIndex != newValue)
        {
            _boundViewModel.SelectedIndex = newValue;
        }
    }

    public int IndexOf(object item)
    {
        if (item == null)
        {
            return -1;
        }

        return _boundViewModel?.Items?.IndexOf(item) ?? -1;
    }

    public int GetItemCount()
    {
        return _boundViewModel?.Items?.Count ?? 0;
    }

    public void BindToViewModel(PreviewItemsViewModel viewModel)
    {
        _boundViewModel?.PropertyChanged -= OnBoundViewModelPropertyChanged;

        _boundViewModel = viewModel;
        if (_boundViewModel != null)
        {
            _boundViewModel.PropertyChanged += OnBoundViewModelPropertyChanged;
            SyncFromBoundViewModel();
        }
    }

    private void OnBoundViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e?.PropertyName) || string.Equals(e.PropertyName, "SelectedIndex", StringComparison.Ordinal) || string.Equals(e.PropertyName, "CurrentItem", StringComparison.Ordinal))
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
}
