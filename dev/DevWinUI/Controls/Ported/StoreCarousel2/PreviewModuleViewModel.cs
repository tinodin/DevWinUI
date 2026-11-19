using System.ComponentModel;

namespace DevWinUI;

public partial class PreviewModuleViewModel : INotifyPropertyChanged
{
    private PreviewType _previewMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<object> Items { get; } = [];

    public ObservableCollection<object> PreviewItems { get; } = [];

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
            RebuildPreviewItems();
            OnPropertyChanged(nameof(PreviewMode));
        }
    }

    internal void RebuildPreviewItems()
    {
        PreviewItems.Clear();

        foreach (var item in Items)
        {
            if (PreviewMode != PreviewType.VideoPlayerSource || item is VideoPlayerSource)
            {
                PreviewItems.Add(item);
            }
        }

        OnPropertyChanged(nameof(PreviewItems));
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
