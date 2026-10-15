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

                OnPropertyChanged("TotalIndex");
            }
        }
    }

    public object CurrentItem
    {
        get => _currentItem;
        set
        {

            if (_currentItem != value)
            {
                _currentItem = value;
                OnPropertyChanged(nameof(CurrentItem));
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
                int oldIndex = _selectedIndex;
                _selectedIndex = value;
                if (_items != null && value >= 0 && value < _items.Count)
                {
                    _currentItem = _items[value];
                }
                OnPropertyChanged("SelectedIndex");
                OnPropertyChanged("Caption");
                OnPropertyChanged("CurrentIndex");
                OnPropertyChanged("TotalIndex");

                if (_items != null && oldIndex >= 0 && oldIndex < _items.Count)
                {
                    if (_items[oldIndex] is VideoPlayerSource oldVideo)
                    {
                        try
                        {
                            if (oldVideo.MediaPlayer?.PlaybackSession?.CanPause == true)
                            {
                                oldVideo.MediaPlayer.Pause();
                            }
                        }
                        catch { }
                    }
                }
            }
        }
    }

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

    public string CurrentIndex => _selectedIndex >= 0 ? (_selectedIndex + 1).ToString() : "1";

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
    private Windows.Media.Playback.MediaPlayer _mediaPlayer;

    public Windows.Media.Playback.MediaPlayer MediaPlayer
    {
        get
        {
            if (_mediaPlayer == null)
            {
                _mediaPlayer = new Windows.Media.Playback.MediaPlayer();
                EnsureMediaSource();
            }
            return _mediaPlayer;
        }
    }

    private Windows.Media.Core.MediaSource _videoSource;
    public Windows.Media.Core.MediaSource VideoMediaSource
    {
        get
        {
            EnsureMediaSource();
            return _videoSource;
        }
    }

    public void EnsureMediaSource()
    {
        if (_videoSource == null && Source != null)
        {
            try
            {
                _videoSource = Windows.Media.Core.MediaSource.CreateFromUri(Source);
                if (_mediaPlayer != null && _mediaPlayer.Source == null)
                {
                    _mediaPlayer.Source = _videoSource;
                }
            }
            catch { }
        }
    }

    public void TeardownPlayer()
    {
        if (_mediaPlayer != null)
        {
            try
            {
                if (_mediaPlayer.PlaybackSession?.PlaybackState != Windows.Media.Playback.MediaPlaybackState.None)
                {
                    _mediaPlayer.Pause();
                }
            }
            catch { }
            try { _mediaPlayer.Source = null; } catch { }
            _mediaPlayer = null;
        }

        _videoSource = null;
    }
}
