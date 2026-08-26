namespace DevWinUI;

public partial class TileImage
{
    public static readonly DependencyProperty FooterTemplateProperty =
        DependencyProperty.Register(nameof(FooterTemplate), typeof(DataTemplate), typeof(TileImage), new PropertyMetadata(null));

    public DataTemplate FooterTemplate
    {
        get => (DataTemplate)GetValue(FooterTemplateProperty);
        set => SetValue(FooterTemplateProperty, value);
    }

    public static readonly DependencyProperty ImageItemProperty =
        DependencyProperty.Register(nameof(ImageItem), typeof(IImageItem), typeof(TileImage), new PropertyMetadata(null, OnImageItemChanged));

    public IImageItem ImageItem
    {
        get => (IImageItem)GetValue(ImageItemProperty);
        set => SetValue(ImageItemProperty, value);
    }

    private static void OnImageItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TileImage)d).ApplyImageItem(e.NewValue as IImageItem);
    }

    public ImageSource Source
    {
        get => (ImageSource)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(TileImage), new PropertyMetadata(null, OnSourceChanged));

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TileImage)d).ApplySource();

    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(TileImage), new PropertyMetadata(Stretch.None));

    public int RequestedImageWidth
    {
        get => (int)GetValue(RequestedImageWidthProperty);
        set => SetValue(RequestedImageWidthProperty, value);
    }

    public static readonly DependencyProperty RequestedImageWidthProperty =
        DependencyProperty.Register(nameof(RequestedImageWidth), typeof(int), typeof(TileImage), new PropertyMetadata(0, OnSizeRequestChanged));

    public int RequestedImageHeight
    {
        get => (int)GetValue(RequestedImageHeightProperty);
        set => SetValue(RequestedImageHeightProperty, value);
    }

    public static readonly DependencyProperty RequestedImageHeightProperty =
        DependencyProperty.Register(nameof(RequestedImageHeight), typeof(int), typeof(TileImage), new PropertyMetadata(0, OnSizeRequestChanged));

    private static void OnSizeRequestChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tile = (TileImage)d;
        if (tile.ImageItem != null)
            tile.ApplyImageItem(tile.ImageItem);
    }

    public bool ShowBackgroundColor
    {
        get => (bool)GetValue(ShowBackgroundColorProperty);
        set => SetValue(ShowBackgroundColorProperty, value);
    }

    public static readonly DependencyProperty ShowBackgroundColorProperty =
        DependencyProperty.Register(nameof(ShowBackgroundColor), typeof(bool), typeof(TileImage), new PropertyMetadata(false));

    public bool ImageProcessed
    {
        get => (bool)GetValue(ImageProcessedProperty);
        set => SetValue(ImageProcessedProperty, value);
    }

    public static readonly DependencyProperty ImageProcessedProperty =
        DependencyProperty.Register(nameof(ImageProcessed), typeof(bool), typeof(TileImage), new PropertyMetadata(false));
}
