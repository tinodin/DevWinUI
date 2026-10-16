namespace DevWinUI;

public partial class ScreenshotDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate AgeRestrictedDataTemplate { get; set; }
    public DataTemplate AgeRestrictedBlockedDataTemplate { get; set; }
    public DataTemplate ScreenshotDataTemplate { get; set; }
    public DataTemplate VideoItemDataTemplate { get; set; }
    public DataTemplate VideoPlayerTemplate { get; set; }

    public bool AgeRestricted { get; set; }
    public bool IsAgeBlocked { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (AgeRestricted)
            return IsAgeBlocked ? AgeRestrictedBlockedDataTemplate ?? AgeRestrictedDataTemplate : AgeRestrictedDataTemplate;

        if (item is VideoPlayerSource)
            return VideoItemDataTemplate ?? VideoPlayerTemplate;

        return ScreenshotDataTemplate;
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
}
