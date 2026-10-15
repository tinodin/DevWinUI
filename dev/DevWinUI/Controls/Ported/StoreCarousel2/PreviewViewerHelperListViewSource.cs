using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace DevWinUI;

public class PreviewViewerHelperListViewSource
{
    private ListViewBase _innerListView;
    private UIElement _hiddenSourceElement;

    public PreviewViewerHelperListViewSource() { }

    public PreviewViewerHelperListViewSource(ListViewBase listView)
    {
        _innerListView = listView;
    }

    public ListViewBase InnerListView
    {
        get => _innerListView;
        set => _innerListView = value;
    }

    public void RestoreSourceVisibility()
    {
        if (_hiddenSourceElement != null)
        {
            _hiddenSourceElement.Opacity = 1;
            _hiddenSourceElement = null;
        }
    }

    public bool GetAnimationSourceElementName(object item, out string name)
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

        if (_innerListView == null)
        {
            return;
        }

        var container = _innerListView.ContainerFromItem(dataItem) as FrameworkElement;
        if (container == null && dataItem is int idx)
        {
            container = _innerListView.ContainerFromIndex(idx) as FrameworkElement;
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

        UIElement source = FindNamedDescendant(container, name) ?? container;
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
            anim = _innerListView.PrepareConnectedAnimation("screenshotForwardAnimation", dataItem, name);
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
        if (_innerListView == null || _innerListView.Items == null || index < 0 || index >= _innerListView.Items.Count)
        {
            return;
        }

        _ = _innerListView.SmoothScrollIntoViewWithIndexAsync(index, ScrollItemPlacement.Default, disableAnimation: true);
    }

    private void FlushItemToEdge(int index, ScrollViewer sv, FrameworkElement container = null)
    {
        if (sv == null || _innerListView == null) return;
        container ??= _innerListView.ContainerFromIndex(index) as FrameworkElement;
        if (container == null) return;

        var content = sv.Content as UIElement;
        if (content == null) return;

        Windows.Foundation.Point origin;
        try
        {
            origin = container.TransformToVisual(content).TransformPoint(new Windows.Foundation.Point(0, 0));
        }
        catch
        {
            return;
        }

        double itemLeft = origin.X;
        double itemRight = itemLeft + container.ActualWidth;

        double viewportWidth = sv.ViewportWidth > 0 ? sv.ViewportWidth : sv.ActualWidth;
        double currentOffset = sv.HorizontalOffset;
        double viewportRight = currentOffset + viewportWidth;

        const double epsilon = 1.0;
        if (itemLeft >= currentOffset - epsilon && itemRight <= viewportRight + epsilon)
        {
            return;
        }

        double targetOffset;
        if (itemLeft < currentOffset)
        {
            targetOffset = itemLeft;
        }
        else
        {
            targetOffset = itemRight - viewportWidth;
        }

        if (targetOffset < 0) targetOffset = 0;
        if (targetOffset > sv.ScrollableWidth) targetOffset = sv.ScrollableWidth;

        sv.ChangeView(targetOffset, null, null, disableAnimation: true);
    }

    public async Task StartBackwardConnectedAnimationAsync(object dataItem)
    {
        if (!GetAnimationSourceElementName(dataItem, out string name))
        {
            return;
        }

        var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("screenshotBackAnimation");
        if (animation == null || _innerListView == null)
        {
            return;
        }

        var container = _innerListView.ContainerFromItem(dataItem) as FrameworkElement;
        if (container == null && dataItem is int idx)
        {
            container = _innerListView.ContainerFromIndex(idx) as FrameworkElement;
        }

        animation.Configuration = new DirectConnectedAnimationConfiguration();

        bool started = false;
        try
        {
            started = await _innerListView.TryStartConnectedAnimationAsync(animation, dataItem, name);
        }
        catch { }

        if (!started && container != null)
        {
            UIElement target = FindNamedDescendant(container, name) ?? container;
            if (target != null)
            {
                target.Opacity = 1;
            }
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

    private static UIElement FindNamedDescendant(DependencyObject root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;

        if (root is FrameworkElement fe && fe.Name == name)
        {
            return fe;
        }

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement childFe && childFe.Name == name)
            {
                return childFe;
            }

            var found = FindNamedDescendant(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null) return null;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            var desc = FindDescendant<T>(child);
            if (desc != null) return desc;
        }
        return null;
    }
}
