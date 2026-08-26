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
}
