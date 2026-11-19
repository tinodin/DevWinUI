using System.ComponentModel;

namespace DevWinUI;

public partial class PreviewViewerHelper : INotifyPropertyChanged
{
    private PreviewType _previewMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PreviewViewer? PreviewViewer { get; set; }

    public PreviewViewerHelperListViewSource? ListViewSource { get; set; }

    public PreviewType PreviewMode
    {
        get => _previewMode;
        set
        {
            if (_previewMode == value)
            {
                return;
            }

            _previewMode = value;
            OnPropertyChanged(nameof(PreviewMode));
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void SetUpPreviewViewer(object dataItem)
    {
        var viewModel = PreviewViewer?.ViewModel;
        if (viewModel?.Items == null || dataItem == null)
        {
            return;
        }

        ListViewSource?.PrepareForwardConnectedAnimation(dataItem);

        viewModel.CurrentItem = dataItem;

        int index = viewModel.Items.IndexOf(dataItem);
        OpenPreviewViewer(index, dataItem);
    }

    public void OnPopupPreviewSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        var viewModel = PreviewViewer?.ViewModel;
        if (viewModel?.Items == null)
        {
            return;
        }

        if (!string.Equals(e?.PropertyName, nameof(PreviewItemsViewModel.SelectedIndex), StringComparison.Ordinal))
        {
            return;
        }

        int index = viewModel.SelectedIndex;
        if (index < 0 || index >= viewModel.Items.Count)
        {
            return;
        }

        ListViewSource?.ScrollToIndex(index);
    }

    public void OpenPreviewViewer(int index, object dataItem)
    {
        if (index < 0)
        {
            return;
        }

        var viewModel = PreviewViewer?.ViewModel;
        if (viewModel?.Items == null)
        {
            return;
        }

        int count = viewModel.Items.Count;
        if (count <= 0)
        {
            return;
        }

        if (count <= index)
        {
            index = 0;
        }

        viewModel.SelectedIndex = index;

        PreviewViewer?.OpenOverlayPopup();
    }
}
