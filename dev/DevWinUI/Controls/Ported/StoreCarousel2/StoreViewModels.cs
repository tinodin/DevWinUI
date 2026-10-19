using System.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevWinUI;

public enum PreviewType
{
    All,
    VideoPlayerSource
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

public partial class PreviewModuleViewModel : ObservableObject
{
    public ObservableCollection<object> Items { get; } = [];

    public ObservableCollection<object> PreviewItems { get; } = [];

    [ObservableProperty]
    public partial PreviewType PreviewMode { get; set; }

    partial void OnPreviewModeChanged(PreviewType value)
    {
        RebuildPreviewItems();
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
}

public partial class PreviewItemsViewModel : ObservableObject
{
    public PreviewItemsViewModel()
    {
        Items = [];
        SelectedIndex = -1;
    }

    [ObservableProperty]
    public partial ObservableCollection<object> Items { get; set; }

    [ObservableProperty]
    public partial object CurrentItem { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Caption), nameof(CurrentIndex), nameof(TotalIndex))]
    public partial int SelectedIndex { get; set; }

    public string Caption
    {
        get
        {
            if (CurrentItem is ScreenshotTileItem screenshot)
            {
                return screenshot.Title ?? string.Empty;
            }
            if (CurrentItem is VideoPlayerSource video)
            {
                return video.Title ?? string.Empty;
            }
            return string.Empty;
        }
    }

    public string CurrentIndex => SelectedIndex >= 0 ? (SelectedIndex + 1).ToString() : "1";

    public string TotalIndex => Items != null ? Items.Count.ToString() : "0";

    partial void OnItemsChanged(ObservableCollection<object> value)
    {
        OnPropertyChanged(nameof(TotalIndex));
    }

    partial void OnSelectedIndexChanged(int oldValue, int newValue)
    {
        if (Items != null && newValue >= 0 && newValue < Items.Count)
        {
            CurrentItem = Items[newValue];
        }

        if (Items != null && oldValue >= 0 && oldValue < Items.Count)
        {
            if (Items[oldValue] is VideoPlayerSource oldVideo)
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

public class ScreenshotTileItem : IImageItem
{
    public object Data { get; set; }
    public Uri ImageUri { get; set; }
    public string Title { get; set; }
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

public class AgeRestrictedTileItem : IImageItem
{
    private readonly IImageItem _imageItem;

    private AgeRestrictedTileItem(IImageItem imageItem)
    {
        _imageItem = imageItem;
    }

    public bool IsAgeBlocked { get; private set; }

    public object Data => _imageItem?.Data;
    public Uri ImageUri => _imageItem?.ImageUri;

    public static AgeRestrictedTileItem Create(IEnumerable items, bool isAgeBlocked)
    {
        return new AgeRestrictedTileItem(FirstImageItem(items)) { IsAgeBlocked = isAgeBlocked };
    }

    private static IImageItem FirstImageItem(IEnumerable items)
    {
        if (items == null)
        {
            return null;
        }

        foreach (var item in items)
        {
            if (item is IImageItem imageItem && item is not VideoPlayerSource)
            {
                return imageItem;
            }
        }

        return null;
    }
}
