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

            element.PosterSource = VideoPlayerSource.ImageSource;
        }
        catch { }
    }

    internal void ApplySource()
    {
        UpdateSource();
    }

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
