using System.Collections.ObjectModel;

namespace DevWinUIGallery.Views;

public sealed partial class StoreCarousel2Page : Page
{
    public ObservableCollection<object> Media { get; } =
    [
        new VideoPlayerSource { ImageUri = new Uri("https://store-images.s-microsoft.com/image/apps.50352.9007199266246365.500da53d-6c15-4c1c-8071-6d065609fbc9.6c4db87d-1ed0-4b9b-b352-9e0add6db9e5"), Title = "Stranger Things S4 Vol. 1 Now Streaming", Source = new Uri("https://cdn.trailers.xboxservices.com/trailers/00000000-0000-0000-0000-000000000000/is/content/microsoftassets/_media_/b86/b86596f5-b921-4670-8e74-3245a275cc47-stream.mp4?utm_medium=fmp4_dash") },
        new ScreenshotTileItem { ImageUri = new Uri("https://store-images.s-microsoft.com/image/apps.35069.9007199266246365.888608e8-85f2-4a52-90d3-72591ee2777e.02ca600e-d925-4cc5-9454-6f5a6de0aab8"), Title = "Screenshot 2" },
        new ScreenshotTileItem { ImageUri = new Uri("https://store-images.s-microsoft.com/image/apps.48760.9007199266246365.888608e8-85f2-4a52-90d3-72591ee2777e.0001b406-8c74-4c9d-bff1-b2d75e16ef3f"), Title = "Screenshot 3" },
        new ScreenshotTileItem { ImageUri = new Uri("https://store-images.s-microsoft.com/image/apps.4114.9007199266246365.888608e8-85f2-4a52-90d3-72591ee2777e.d53db9c1-3976-42a0-a755-08bd811f74d6"), Title = "Screenshot 4" }
    ];

    public StoreCarousel2Page()
    {
        InitializeComponent();
    }
}
