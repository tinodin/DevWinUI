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

public class PreviewModuleViewModel
{
    public object CurrentItem { get; set; }
    public ObservableCollection<object> Items { get; } = [];
}

public partial class PreviewItemsViewModel : System.ComponentModel.INotifyPropertyChanged
{
    private object _currentItem;

    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<object> Items
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalIndex));
            }
        }
    } = [];

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
        get;
        set
        {
            if (field != value)
            {
                int oldIndex = field;
                field = value;
                if (Items != null && value >= 0 && value < Items.Count)
                {
                    _currentItem = Items[value];
                }
                OnPropertyChanged(nameof(SelectedIndex));
                OnPropertyChanged(nameof(Caption));
                OnPropertyChanged(nameof(CurrentIndex));
                OnPropertyChanged(nameof(TotalIndex));

                if (Items != null && oldIndex >= 0 && oldIndex < Items.Count)
                {
                    if (Items[oldIndex] is VideoPlayerSource oldVideo)
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
    } = -1;

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

    public string CurrentIndex => SelectedIndex >= 0 ? (SelectedIndex + 1).ToString() : "1";

    public string TotalIndex => Items != null ? Items.Count.ToString() : "0";

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}

public class ScreenshotTileItem : IImageItem
{
    public object Data { get; set; }
    public Uri ImageUri { get; set; }
    public string Title { get; set; }

    public ImageSource ImageSource
    {
        get
        {
            if (field == null && ImageUri != null)
                field = new BitmapImage(ImageUri) { CreateOptions = BitmapCreateOptions.None };
            return field;
        }
    }
}

public class VideoPlayerSource : IImageItem
{
    public object Data { get; set; }
    public Uri Source { get; set; }
    public Uri ImageUri { get; set; }
    public string Title { get; set; }

    public ImageSource ImageSource
    {
        get
        {
            if (field == null && ImageUri != null)
                field = new BitmapImage(ImageUri)
                {
                    CreateOptions = BitmapCreateOptions.None,
                    DecodePixelHeight = 316
                };
            return field;
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
