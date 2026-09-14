using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace DevWinUI;

/// <summary>
/// Connects <see cref="ScreenshotsViewer"/>'s inner list with connected animation source
/// element lookup and instant selection synchronization. Ported faithfully from Microsoft
/// Store PDP's <c>PreviewViewerHelperListViewSource</c>.
/// </summary>
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

    /// <summary>
    /// Restores visibility/opacity of any source element hidden during connected animation.
    /// </summary>
    public void RestoreSourceVisibility()
    {
        if (_hiddenSourceElement != null)
        {
            _hiddenSourceElement.Opacity = 1;
            _hiddenSourceElement = null;
        }
    }

    /// <summary>
    /// Maps data item to its corresponding visual element name inside item templates.
    /// Recovered from <c>0x18209EEE0</c>.
    /// </summary>
    public bool GetAnimationSourceElementName(object item, out string name)
    {
        name = null;
        if (item is ScreenshotTileItem)
        {
            name = "TileImageRootGrid"; // [STR 0x18209EF3B]
        }
        else if (item is VideoPlayerSource)
        {
            name = "VideoPlayerPreviewGrid"; // [STR 0x18209EF32]
        }
        else if (item != null && item.GetType().Name == "VideoThumbnailItem")
        {
            name = "VideoPreviewImage"; // [STR 0x18209EF0F]
        }
        else
        {
            name = string.Empty; // [STR 0x18209EF29]
        }

        return !string.IsNullOrEmpty(name);
    }

    /// <summary>
    /// Prepares forward connected animation from the strip tile descendant.
    /// Recovered from <c>0x18209EDA0</c> (which calls <c>ListViewBase.PrepareConnectedAnimation</c> <c>0x1804FD330</c>).
    /// </summary>
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

        // Restore any previously hidden source element
        RestoreSourceVisibility();

        // Lock container dimensions so that the item does not collapse during connected animation in WinUI 3
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

        // In MS Store 0x18209EDA0: calls ListViewBase.PrepareConnectedAnimation (0x1804fd330)
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

        // Hide the source element in the strip immediately after snapshot creation
        // so that it does not remain visible behind PreviewViewer's translucent overlay.
        if (source != null)
        {
            source.Opacity = 0;
            _hiddenSourceElement = source;
        }
    }

    /// <summary>
    /// Smoothly updates selection and scrolls the strip item into view with edge alignment.
    /// Recovered from <c>0x18209EE90</c> / <c>0x1821B9980</c> -> <c>0x182D8EA00</c> (<c>sub_180080120</c>).
    /// </summary>
    public void ScrollToIndex(int index)
    {
        UpdateSelectedIndex(index);
    }

    /// <summary>
    /// Method-name recovered from line 107 string <c>0x186085FE8</c> (<c>UpdateSelectedIndex</c>).
    /// Reconstructed faithfully from MS Store <c>sub_180080120</c> (which calculates item position relative to ScrollViewer
    /// and aligns the item flush to the edge if it is not fully in view).
    /// </summary>
    public void UpdateSelectedIndex(int index)
    {
        if (_innerListView == null || _innerListView.Items == null || index < 0 || index >= _innerListView.Items.Count)
        {
            return;
        }

        // Recovered from sub_1821B9980 invoking sub_182D8EA00 (SmoothScrollIntoViewWithIndexAsync) with itemPlacement=Default, disableAnimation=true
        _ = _innerListView.SmoothScrollIntoViewWithIndexAsync(index, ScrollItemPlacement.Default, disableAnimation: true);
    }

    /// <summary>
    /// Aligns the selected item flush with the edge of the viewport matching MS Store <c>sub_180080120</c>.
    /// </summary>
    private void FlushItemToEdge(int index, ScrollViewer sv, FrameworkElement container = null)
    {
        if (sv == null || _innerListView == null) return;
        container ??= _innerListView.ContainerFromIndex(index) as FrameworkElement;
        if (container == null) return;

        var content = sv.Content as UIElement;
        if (content == null) return;

        // Container position relative to scroll content (v9 in sub_180080120)
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

        // If item is already fully visible (sub_180080120 lines 412-416)
        const double epsilon = 1.0;
        if (itemLeft >= currentOffset - epsilon && itemRight <= viewportRight + epsilon)
        {
            return;
        }

        // Fit selected item in view with edge (sub_180080120 lines 417-438 case 0):
        // Closer to left / scrolled past left -> align flush to left edge
        // Closer to right / scrolled past right -> align flush to right edge
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

    /// <summary>
    /// Initiates backward connected animation to the strip tile visual.
    /// Recovered from <c>0x1821B9660</c> (which calls <c>ListViewBase.TryStartConnectedAnimationAsync</c> <c>0x1804FCFD0</c>).
    /// </summary>
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

        // Restore NaN width/height if set during prepare
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

