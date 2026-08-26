namespace DevWinUI;

public sealed partial class PreviewViewerScreenshotItem : UserControl
{
    public static readonly DependencyProperty TileItemProperty =
        DependencyProperty.Register(nameof(TileItem), typeof(ScreenshotTileItem), typeof(PreviewViewerScreenshotItem), new PropertyMetadata(null, OnTileItemChanged));

    public ScreenshotTileItem TileItem
    {
        get => (ScreenshotTileItem)GetValue(TileItemProperty);
        set => SetValue(TileItemProperty, value);
    }

    public static readonly DependencyProperty ContentElementProperty =
        DependencyProperty.Register(nameof(ContentElement), typeof(FrameworkElement), typeof(PreviewViewerScreenshotItem), new PropertyMetadata(null));

    public FrameworkElement ContentElement
    {
        get => (FrameworkElement)GetValue(ContentElementProperty);
        set => SetValue(ContentElementProperty, value);
    }

    public PreviewViewerScreenshotItem()
    {
        InitializeComponent();
        Loaded += (s, e) => UpdateImage();
    }

    private static void OnTileItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((PreviewViewerScreenshotItem)d).UpdateImage();

    private void UpdateImage()
    {
        var img = FindName("Image") as Image;
        if (img != null && TileItem?.ImageUri != null)
            img.Source = new BitmapImage(TileItem.ImageUri);
    }
}
