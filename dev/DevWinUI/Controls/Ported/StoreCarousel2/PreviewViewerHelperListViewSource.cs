namespace DevWinUI;

public class PreviewViewerHelperListViewSource
{
    private UIElement _hiddenSourceElement;

    public PreviewViewerHelperListViewSource(ListViewBase listView)
    {
        InnerListView = listView;
    }

    public ListViewBase InnerListView { get; set; }

    public void RestoreSourceVisibility()
    {
        _hiddenSourceElement?.Opacity = 1;
        _hiddenSourceElement = null;
    }

    public static bool GetAnimationSourceElementName(object item, out string name)
    {
        name = null;
        if (item is ScreenshotTileItem)
        {
            name = "TileImageRootGrid";
        }
        else if (item is VideoPlayerSource)
        {
            name = "VideoPlayerPreviewGrid";
        }
        else if (item != null && item.GetType().Name == "VideoThumbnailItem")
        {
            name = "VideoPreviewImage";
        }
        else
        {
            name = string.Empty;
        }

        return !string.IsNullOrEmpty(name);
    }

    public void PrepareForwardConnectedAnimation(object dataItem)
    {
        if (!GetAnimationSourceElementName(dataItem, out string name))
        {
            return;
        }

        if (InnerListView == null)
        {
            return;
        }

        var container = InnerListView.ContainerFromItem(dataItem) as FrameworkElement;
        if (container == null && dataItem is int idx)
        {
            container = InnerListView.ContainerFromIndex(idx) as FrameworkElement;
        }

        if (container == null || !IsVisibleWithNonZeroSize(container))
        {
            return;
        }

        RestoreSourceVisibility();

        if (container.ActualWidth > 0 && double.IsNaN(container.Width))
        {
            container.Width = container.ActualWidth;
        }
        if (container.ActualHeight > 0 && double.IsNaN(container.Height))
        {
            container.Height = container.ActualHeight;
        }

        UIElement source = container.FindDescendantOrSelf(name) ?? container;
        if (source is FrameworkElement sourceFe)
        {
            if (sourceFe.ActualWidth > 0 && double.IsNaN(sourceFe.Width))
            {
                sourceFe.Width = sourceFe.ActualWidth;
            }
            if (sourceFe.ActualHeight > 0 && double.IsNaN(sourceFe.Height))
            {
                sourceFe.Height = sourceFe.ActualHeight;
            }
        }

        ConnectedAnimation anim = null;

        try
        {
            anim = InnerListView.PrepareConnectedAnimation("screenshotForwardAnimation", dataItem, name);
        }
        catch { }

        if (anim == null && source != null)
        {
            var service = ConnectedAnimationService.GetForCurrentView();
            anim = service.PrepareToAnimate("screenshotForwardAnimation", source);
        }

        if (anim != null)
        {
            anim.Configuration = new DirectConnectedAnimationConfiguration();
        }

        if (source != null)
        {
            source.Opacity = 0;
            _hiddenSourceElement = source;
        }
    }

    public void ScrollToIndex(int index)
    {
        UpdateSelectedIndex(index);
    }

    public void UpdateSelectedIndex(int index)
    {
        if (InnerListView == null || InnerListView.Items == null || index < 0 || index >= InnerListView.Items.Count)
        {
            return;
        }

        _ = InnerListView.SmoothScrollIntoViewWithIndexAsync(index, ScrollItemPlacement.Default, disableAnimation: true);
    }

    public async Task StartBackwardConnectedAnimationAsync(object dataItem)
    {
        if (!GetAnimationSourceElementName(dataItem, out string name))
        {
            return;
        }

        var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("screenshotBackAnimation");
        if (animation == null || InnerListView == null)
        {
            return;
        }

        var container = InnerListView.ContainerFromItem(dataItem) as FrameworkElement;
        if (container == null && dataItem is int idx)
        {
            container = InnerListView.ContainerFromIndex(idx) as FrameworkElement;
        }

        animation.Configuration = new DirectConnectedAnimationConfiguration();

        bool started = false;
        try
        {
            started = await InnerListView.TryStartConnectedAnimationAsync(animation, dataItem, name);
        }
        catch { }

        if (!started && container != null)
        {
            UIElement target = container.FindDescendantOrSelf(name) ?? container;
            target?.Opacity = 1;
            animation.TryStart(target);
        }

        RestoreSourceVisibility();

        if (container != null)
        {
            container.ClearValue(FrameworkElement.WidthProperty);
            container.ClearValue(FrameworkElement.HeightProperty);
        }

        await Task.CompletedTask;
    }

    private static bool IsVisibleWithNonZeroSize(FrameworkElement element)
    {
        if (element == null) return false;
        return element.Visibility == Visibility.Visible && element.ActualWidth > 0 && element.ActualHeight > 0;
    }

}
