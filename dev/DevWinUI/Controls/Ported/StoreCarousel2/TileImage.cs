namespace DevWinUI;

[TemplatePart(Name = "RootGrid", Type = typeof(Grid))]
[TemplatePart(Name = "MainImage", Type = typeof(Image))]
public partial class TileImage : Control
{
    private Grid _rootGrid;
    private Image _image;
    private ImageSource _currentSource;

    public TileImage()
    {
        DefaultStyleKey = typeof(TileImage);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_image != null)
        {
            _image.ImageOpened -= OnImageOpened;
            _image.ImageFailed -= OnImageFailed;
        }

        _rootGrid = GetTemplateChild("RootGrid") as Grid;
        _image = GetTemplateChild("MainImage") as Image;

        if (_image != null)
        {
            _image.ImageOpened += OnImageOpened;
            _image.ImageFailed += OnImageFailed;
            if (_currentSource != null)
            {
                _image.Source = _currentSource;
                _image.Stretch = Stretch;
            }
        }

        if (ImageItem != null)
            ApplyImageItem(ImageItem);
        else
            UpdateVisualState();
    }

    internal void UpdateImageSize()
    {

    }

    internal void ApplyImageItem(IImageItem item)
    {
        if (item?.ImageUri != null)
        {
            var bmp = new BitmapImage(item.ImageUri)
            {
                CreateOptions = BitmapCreateOptions.None,
                DecodePixelHeight = RequestedImageHeight > 0 ? RequestedImageHeight : 0,
                DecodePixelWidth = RequestedImageWidth > 0 ? RequestedImageWidth : 0
            };
            _currentSource = bmp;
            if (_image != null)
            {
                _image.Source = _currentSource;
                _image.Stretch = Stretch;
            }
        }
        else
        {
            _currentSource = null;
            if (_image != null)
                _image.Source = null;
        }

        UpdateImageSize();

        if (_rootGrid != null)
        {
            _rootGrid.Opacity = 0;
        }

        ImageProcessed = _currentSource == null && item == null;
        InvalidateMeasure();

        if ((_currentSource == null && item == null) || !ShowBackgroundColor)
        {
            GoToColoredBackplate();
        }
    }

    private void UpdateVisualState()
    {
        if (_rootGrid != null)
            _rootGrid.Opacity = 0;

        ImageProcessed = _currentSource == null && ImageItem == null;
        InvalidateMeasure();

        if ((_currentSource == null && ImageItem == null) || !ShowBackgroundColor)
            GoToColoredBackplate();
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        ImageProcessed = true;
        InvalidateMeasure();

        if (_rootGrid != null && _currentSource != null)
        {
            var fadeIn = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(300))
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeIn, _rootGrid);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeIn, "Opacity");

            var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
            storyboard.Children.Add(fadeIn);
            storyboard.Begin();
        }

        GoToDefaultBackplate();
    }

    private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        ImageProcessed = true;
        InvalidateMeasure();
        GoToColoredBackplate();
    }

    private void GoToDefaultBackplate()
    {
        VisualStateManager.GoToState(this, "DefaultBackplateVisualState", true);
    }

    private void GoToColoredBackplate()
    {
        VisualStateManager.GoToState(this, "ColoredBackplateVisualState", true);
    }
}
