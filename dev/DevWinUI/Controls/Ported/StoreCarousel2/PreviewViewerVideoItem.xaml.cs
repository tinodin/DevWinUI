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
        var element = VideoPlayerItemElement;
        if (element == null || VideoPlayerSource == null)
            return;

        try
        {
            element.SetMediaPlayer(VideoPlayerSource.MediaPlayer);

            // The video item is a bare MediaPlayerElement, so nothing is rendered until the
            // media has decoded enough to produce its first frame - which over the network
            // takes well over a second, leaving the viewer blank on open.
            // Show the item's own preview image (the same ImageSource the in-page
            // ScreenshotsViewer tile renders, already decoded by the carousel) as the
            // element's poster. MediaPlayerPresenter drops the poster by itself as soon as
            // the media produces a frame, so the video takes over with no extra work.
            element.PosterSource = VideoPlayerSource.ImageSource;
        }
        catch { }
    }

    /// <summary>
    /// Connects the VideoPlayerSource's MediaPlayer to the container's MediaPlayerElement.
    /// Recovers Store's <c>sub_182172450</c> (which calls <c>IMediaPlayerElement::SetMediaPlayer</c> at slot 21 / 0x1804FD5F0).
    /// </summary>
    internal void ApplySource()
    {
        UpdateSource();
    }

    /// <summary>
    /// Per-video teardown on close (Store <c>sub_1823F0D20</c> -> <c>sub_1823F6200</c>).
    /// </summary>
    internal void ResetSource()
    {
        try
        {
            var element = VideoPlayerItemElement;
            if (element != null)
            {
                element.PosterSource = null;
                element.SetMediaPlayer(null);
            }
        }
        catch { }

        VideoPlayerSource?.TeardownPlayer();
    }
}
