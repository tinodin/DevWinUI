namespace DevWinUI;

public partial class ScreenshotDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate AgeRestrictedDataTemplate { get; set; }
    public DataTemplate ScreenshotDataTemplate { get; set; }
    public DataTemplate VideoItemDataTemplate { get; set; }
    public DataTemplate VideoPlayerTemplate { get; set; }

    // Store selects this template from item data; StoreCarousel2 also supports a
    // whole-module override through its AgeRestricted dependency property.
    public bool AgeRestricted { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (AgeRestricted || item is IAgeRestrictedItem { AgeRestricted: true })
            return AgeRestrictedDataTemplate;

        if (item is VideoPlayerSource)
            return VideoItemDataTemplate ?? VideoPlayerTemplate;

        return ScreenshotDataTemplate;
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
}
