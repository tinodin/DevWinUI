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
}

public partial class PreviewItemsViewModel : System.ComponentModel.INotifyPropertyChanged
{
    private ObservableCollection<object> _items = new();
    private object _currentItem;

    public ObservableCollection<object> Items
    {
        get => _items;
        set { _items = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Items))); }
    }

    public object CurrentItem
    {
        get => _currentItem;
        set { _currentItem = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(CurrentItem))); }
    }

    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
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
