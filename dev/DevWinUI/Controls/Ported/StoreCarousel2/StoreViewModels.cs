namespace DevWinUI;

public enum PreviewType
{
    None,
    Screenshot,
    Video
}

public interface ICardDataModel
{
    object Data { get; }
}

public interface IImageItem
{
    object Data { get; }
    Uri ImageUri { get; }
}

public interface IAgeRestrictedItem
{
    bool AgeRestricted { get; }
}

public class PreviewModuleViewModel
{
    public object CurrentItem { get; set; }
    public ObservableCollection<object> Items { get; } = new();

    public event EventHandler<object> FullscreenParentShown;

    public void OnFullscreenParentShown(object dataItem)
    {
        FullscreenParentShown?.Invoke(this, dataItem);
    }
}

public partial class PreviewItemsViewModel : System.ComponentModel.INotifyPropertyChanged
{
    private ObservableCollection<object> _items = new();
    private object _currentItem;
    private int _selectedIndex = -1;

    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<object> Items
    {
        get => _items;
        set
        {
            if (_items != value)
            {
                _items = value;
                OnPropertyChanged(nameof(Items));
                // Store TotalIndex is derived from items count (PropertyChanged handler
                // 0x18225A1B0 reads count via sub_18097B930 on items); notify like SelectedIndex setter.
                OnPropertyChanged("TotalIndex");
            }
        }
    }

    public object CurrentItem
    {
        get => _currentItem;
        set
        {
            // Store SetUpPreviewViewer writes CurrentItem via raw put sub_1830F19C0(list+0x30)
            // only — it does NOT update SelectedIndex. SelectedIndex is written solely by
            // OpenPreviewViewer → 0x1823F0170. Coupling IndexOf here would pre-set SelectedIndex
            // so the open-path setter becomes a no-op (no PropertyChanged → FlipView stays at 0).
            if (_currentItem != value)
            {
                _currentItem = value;
                OnPropertyChanged(nameof(CurrentItem));
            }
        }
    }

    /// <summary>
    /// Store setter <c>0x1823F0170</c>: write only when value changes, then raise
    /// PropertyChanged for SelectedIndex, Caption, CurrentIndex, TotalIndex.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value)
            {
                _selectedIndex = value;
                if (_items != null && value >= 0 && value < _items.Count)
                {
                    _currentItem = _items[value];
                }
                OnPropertyChanged("SelectedIndex");
                OnPropertyChanged("Caption");
                OnPropertyChanged("CurrentIndex");
                OnPropertyChanged("TotalIndex");
            }
        }
    }

    /// <summary>
    /// Store getter <c>0x1823F03D0</c>: always derived from the item at SelectedIndex
    /// (no separate cached caption field).
    /// </summary>
    public string Caption
    {
        get
        {
            if (_currentItem is ScreenshotTileItem screenshot)
            {
                return screenshot.Title ?? string.Empty;
            }
            if (_currentItem is VideoPlayerSource video)
            {
                return video.Title ?? string.Empty;
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// Store getter <c>0x1823F0480</c>: <c>ToString(SelectedIndex + 1)</c> via <c>sub_18097B930</c>.
    /// </summary>
    public string CurrentIndex => _selectedIndex >= 0 ? (_selectedIndex + 1).ToString() : "1";

    /// <summary>
    /// Store PropertyChanged path reads items collection count (not a stored string).
    /// </summary>
    public string TotalIndex => _items != null ? _items.Count.ToString() : "0";

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}

public class ScreenshotTileItem : IImageItem, IAgeRestrictedItem
{
    public object Data { get; set; }
    public Uri ImageUri { get; set; }
    public string Title { get; set; }
    public bool AgeRestricted { get; set; }
    private ImageSource _imageSource;
    public ImageSource ImageSource
    {
        get
        {
            if (_imageSource == null && ImageUri != null)
                _imageSource = new BitmapImage(ImageUri) { CreateOptions = BitmapCreateOptions.None };
            return _imageSource;
        }
    }
}

public class VideoPlayerSource : IImageItem, IAgeRestrictedItem
{
    public object Data { get; set; }
    public Uri Source { get; set; }
    public Uri ImageUri { get; set; }
    public string Title { get; set; }
    public bool AgeRestricted { get; set; }
    private ImageSource _imageSource;
    public ImageSource ImageSource
    {
        get
        {
            if (_imageSource == null && ImageUri != null)
                _imageSource = new BitmapImage(ImageUri)
                {
                    CreateOptions = BitmapCreateOptions.None,
                    DecodePixelHeight = 316
                };
            return _imageSource;
        }
    }
    private Windows.Media.Core.MediaSource _videoSource;
    public Windows.Media.Core.MediaSource VideoMediaSource
    {
        get
        {
            if (_videoSource == null && Source != null)
                try { _videoSource = Windows.Media.Core.MediaSource.CreateFromUri(Source); } catch { }
            return _videoSource;
        }
    }
}
