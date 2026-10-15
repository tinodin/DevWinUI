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
    /// Also pauses previous video item when navigating away.
    /// </summary>
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

                // Store sub_1823F0170: Pause previous video item when navigating away
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
    private Windows.Media.Playback.MediaPlayer _mediaPlayer;
    /// <summary>
    /// Store VideoPlayerSource player instance (field +0x18 / sub_18033C2F0).
    /// Kept alive for the duration of the preview session so navigation does not reload the media.
    /// </summary>
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

    /// <summary>
    /// Store sub_1823F6200: per-video teardown on close. Pauses playback, nulls the source,
    /// and dereferences the player and media source so a reopen starts fresh from the beginning.
    /// Avoids calling Dispose() directly on MediaPlayer or MediaSource to prevent native RO_E_CLOSED crashes
    /// in the WinUI 3 compositor when visual containers are detached.
    /// </summary>
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
