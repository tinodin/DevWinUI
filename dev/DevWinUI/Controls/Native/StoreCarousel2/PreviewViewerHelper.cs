using System.ComponentModel;

namespace DevWinUI;

public partial class PreviewViewerHelper : INotifyPropertyChanged
{
    public PreviewViewer PreviewViewer { get; set; }

    public PreviewPopupHost Popup
    {
        get;
        set
        {
            if (field?.List != null)
            {
                field.List.PropertyChanged -= OnPopupPreviewSelectionChanged;
            }
            field = value;
            if (field?.List != null)
            {
                field.List.PropertyChanged += OnPopupPreviewSelectionChanged;
            }
        }
    }

    public PreviewViewerHelperListViewSource ListViewSource { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    public PreviewModuleViewModel ViewModel
    {
        get;
        set
        {
            if (ReferenceEquals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged(nameof(ViewModel));
        }
    }

    public PreviewType PreviewMode
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged(nameof(PreviewMode));
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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

public partial class PreviewListFacade : INotifyPropertyChanged
{
    private PreviewItemsViewModel _boundViewModel;
    private bool _syncingFromBound;

    public event PropertyChangedEventHandler PropertyChanged;

    public object CurrentItem
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
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
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }
    }

    public int SelectedIndex
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
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
    } = -1;

    public ItemCollection Items { get; set; }

    public int IndexOf(object item)
    {
        if (item == null)
        {
            return -1;
        }

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

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
