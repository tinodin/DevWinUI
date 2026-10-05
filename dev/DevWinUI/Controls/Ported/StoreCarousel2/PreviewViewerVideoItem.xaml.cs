using Windows.Media.Core;

namespace DevWinUI;

public sealed partial class PreviewViewerVideoItem : UserControl
{
    public static readonly DependencyProperty VideoPlayerSourceProperty =
        DependencyProperty.Register(nameof(VideoPlayerSource), typeof(VideoPlayerSource), typeof(PreviewViewerVideoItem), new PropertyMetadata(null, OnSourceChanged));

    public VideoPlayerSource VideoPlayerSource
    {
        get => (VideoPlayerSource)GetValue(VideoPlayerSourceProperty);
        set => SetValue(VideoPlayerSourceProperty, value);
    }

    public static readonly DependencyProperty ContentElementProperty =
        DependencyProperty.Register(nameof(ContentElement), typeof(FrameworkElement), typeof(PreviewViewerVideoItem), new PropertyMetadata(null));

    public FrameworkElement ContentElement
    {
        get => (FrameworkElement)GetValue(ContentElementProperty);
        set => SetValue(ContentElementProperty, value);
    }

    public PreviewViewerVideoItem()
    {
        InitializeComponent();
        Loaded += (s, e) => UpdateSource();
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((PreviewViewerVideoItem)d).UpdateSource();

    private void UpdateSource()
    {
        var player = FindName("VideoPlayerItemElement") as MediaPlayerElement;
        if (player != null && VideoPlayerSource?.Source != null)
            player.Source = MediaSource.CreateFromUri(VideoPlayerSource.Source);
    }

    /// <summary>
    /// Re-assigns the media source, i.e. Store's <c>sub_182172450</c> putting the item's media
    /// holder onto the media element. Called when the viewer becomes active on a video item
    /// (open and selection change) so a reopened clip starts from the beginning.
    /// </summary>
    internal void ApplySource()
    {
        var player = FindName("VideoPlayerItemElement") as MediaPlayerElement;
        if (player != null && VideoPlayerSource?.Source != null)
        {
            player.Source = MediaSource.CreateFromUri(VideoPlayerSource.Source);
        }
    }

    /// <summary>
    /// Stops playback and releases the media source. The port of Store's per-video teardown
    /// on close (<c>sub_1823F0D20</c> → <c>sub_1823F6200</c>, which nulls a property on every
    /// <c>VideoPlayerSource</c> item's media object). Clearing the source is what makes the
    /// next open restart the clip instead of resuming it.
    /// </summary>
    internal void ResetSource()
    {
        var player = FindName("VideoPlayerItemElement") as MediaPlayerElement;
        if (player == null)
            return;

        try { player.MediaPlayer?.Pause(); } catch { }
        try { player.Source = null; } catch { }
    }
}
