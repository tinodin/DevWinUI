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
                element.SetMediaPlayer(null);
            }
        }
        catch { }

        VideoPlayerSource?.TeardownPlayer();
    }
}
