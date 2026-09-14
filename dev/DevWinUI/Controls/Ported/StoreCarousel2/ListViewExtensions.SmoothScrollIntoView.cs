// ================================================================================
// RECONSTRUCTED from Microsoft Store (NativeAOT C#) WinStore.App.dll
// Addresses:
//   sub_180080120: <SmoothScrollIntoViewWithIndexAsync>d__22.MoveNext()
//   sub_18007FC00: <ChangeViewAsync>d__24.MoveNext()
//   sub_182D8EA00: Async state machine invocation wrapper for SmoothScrollIntoViewWithIndexAsync
//   sub_182D8E7F0: Async state machine invocation wrapper for ChangeViewAsync
// Frozen string table literal:
//   0x1833e5134: ",<ChangeViewAsync>d__24R<SmoothScrollIntoViewWithIndexAsync>d__22"
// ================================================================================

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace DevWinUI;

/// <summary>
/// Sets the item placement after scrolling.
/// Decompiled from MS Store <c>sub_180080120</c> switch table (lines 417-464):
/// Case 0 = Default (flush edge alignment),
/// Case 1 = Left, Case 2 = Top,
/// Case 3 = Center ((listViewWidth - itemWidth) * 0.5),
/// Case 4 = Right, Case 5 = Bottom.
/// </summary>
public enum ScrollItemPlacement
{
    Default = 0,
    Left = 1,
    Top = 2,
    Center = 3,
    Right = 4,
    Bottom = 5
}

/// <summary>
/// Smooth scroll extension methods for <see cref="ListViewBase"/> and <see cref="ScrollViewer"/>.
/// Implements the exact algorithm recovered from Microsoft Store binary (<c>sub_180080120</c>).
/// </summary>
public static partial class ListViewExtensions
{
    /// <summary>
    /// Smoothly scrolls the list to bring the specified item index into view.
    /// Recovered from <c>sub_180080120</c> (<c>&lt;SmoothScrollIntoViewWithIndexAsync&gt;d__22</c>).
    /// </summary>
    /// <param name="listViewBase">The list to scroll.</param>
    /// <param name="index">The item index to bring into view.</param>
    /// <param name="itemPlacement">The target alignment (e.g. <see cref="ScrollItemPlacement.Center"/> for keyboard navigation).</param>
    /// <param name="disableAnimation">Whether animation is disabled (true for instant sync, false for smooth scroll).</param>
    /// <param name="scrollIfVisible">Whether to scroll if the item is already visible.</param>
    /// <param name="additionalHorizontalOffset">Optional horizontal offset delta.</param>
    /// <param name="additionalVerticalOffset">Optional vertical offset delta.</param>
    public static async Task SmoothScrollIntoViewWithIndexAsync(
        this ListViewBase listViewBase,
        int index,
        ScrollItemPlacement itemPlacement = ScrollItemPlacement.Default,
        bool disableAnimation = false,
        bool scrollIfVisible = true,
        int additionalHorizontalOffset = 0,
        int additionalVerticalOffset = 0)
    {
        if (listViewBase == null || listViewBase.Items.Count == 0)
        {
            return;
        }

        if (index > listViewBase.Items.Count - 1)
        {
            index = listViewBase.Items.Count - 1;
        }

        if (index < -listViewBase.Items.Count)
        {
            index = -listViewBase.Items.Count;
        }

        index = (index < 0) ? (index + listViewBase.Items.Count) : index;

        var scrollViewer = FindDescendant<ScrollViewer>(listViewBase);
        if (scrollViewer == null)
        {
            return;
        }

        var selectorItem = listViewBase.ContainerFromIndex(index) as SelectorItem;
        bool isVirtualizing = false;
        double previousXOffset = scrollViewer.HorizontalOffset;
        double previousYOffset = scrollViewer.VerticalOffset;

        // If selectorItem is null, realization is needed via ScrollIntoView
        if (selectorItem == null)
        {
            isVirtualizing = true;
            previousXOffset = scrollViewer.HorizontalOffset;
            previousYOffset = scrollViewer.VerticalOffset;

            var tcs = new TaskCompletionSource<object>();
            void ViewChanged(object sender, ScrollViewerViewChangedEventArgs args) => tcs.TrySetResult(null);

            try
            {
                scrollViewer.ViewChanged += ViewChanged;
                listViewBase.ScrollIntoView(listViewBase.Items[index], ScrollIntoViewAlignment.Leading);
                await tcs.Task;
            }
            finally
            {
                scrollViewer.ViewChanged -= ViewChanged;
            }

            selectorItem = listViewBase.ContainerFromIndex(index) as SelectorItem;
        }

        if (selectorItem == null)
        {
            return;
        }

        var scrollContent = scrollViewer.Content as UIElement;
        if (scrollContent == null)
        {
            return;
        }

        Point position;
        try
        {
            var transform = selectorItem.TransformToVisual(scrollContent);
            position = transform.TransformPoint(new Point(0, 0));
        }
        catch
        {
            return;
        }

        // Restore original scroll offset if we had to realize a virtualized item
        if (isVirtualizing)
        {
            await scrollViewer.ChangeViewCoreAsync(previousXOffset, previousYOffset, zoomFactor: null, disableAnimation: true);
        }

        double listViewWidth = listViewBase.ActualWidth;
        double itemWidth = selectorItem.ActualWidth;
        double listViewHeight = listViewBase.ActualHeight;
        double itemHeight = selectorItem.ActualHeight;

        previousXOffset = scrollViewer.HorizontalOffset;
        previousYOffset = scrollViewer.VerticalOffset;

        double minXPosition = position.X - listViewWidth + itemWidth;
        double minYPosition = position.Y - listViewHeight + itemHeight;
        double maxXPosition = position.X;
        double maxYPosition = position.Y;

        double finalXPosition;
        double finalYPosition;

        // If the item is already in view and scrollIfVisible is false, do not scroll
        if (!scrollIfVisible
            && (previousXOffset <= maxXPosition && previousXOffset >= minXPosition)
            && (previousYOffset <= maxYPosition && previousYOffset >= minYPosition))
        {
            finalXPosition = previousXOffset;
            finalYPosition = previousYOffset;
        }
        else
        {
            switch (itemPlacement)
            {
                case ScrollItemPlacement.Default:
                    // Minimal edge fit matching sub_180080120 lines 420-432
                    if (previousXOffset <= maxXPosition && previousXOffset >= minXPosition)
                    {
                        finalXPosition = previousXOffset + additionalHorizontalOffset;
                    }
                    else if (Math.Abs(previousXOffset - minXPosition) < Math.Abs(previousXOffset - maxXPosition))
                    {
                        finalXPosition = minXPosition + additionalHorizontalOffset;
                    }
                    else
                    {
                        finalXPosition = maxXPosition + additionalHorizontalOffset;
                    }

                    if (previousYOffset <= maxYPosition && previousYOffset >= minYPosition)
                    {
                        finalYPosition = previousYOffset + additionalVerticalOffset;
                    }
                    else if (Math.Abs(previousYOffset - minYPosition) < Math.Abs(previousYOffset - maxYPosition))
                    {
                        finalYPosition = minYPosition + additionalVerticalOffset;
                    }
                    else
                    {
                        finalYPosition = maxYPosition + additionalVerticalOffset;
                    }
                    break;

                case ScrollItemPlacement.Left:
                    finalXPosition = maxXPosition + additionalHorizontalOffset;
                    finalYPosition = previousYOffset + additionalVerticalOffset;
                    break;

                case ScrollItemPlacement.Top:
                    finalXPosition = previousXOffset + additionalHorizontalOffset;
                    finalYPosition = maxYPosition + additionalVerticalOffset;
                    break;

                case ScrollItemPlacement.Center:
                    // Exact calculation matching sub_180080120 lines 448-449:
                    // finalXPosition = position.X - (listViewWidth - itemWidth) * 0.5 + additionalHorizontalOffset
                    double centreX = (listViewWidth - itemWidth) * 0.5;
                    double centreY = (listViewHeight - itemHeight) * 0.5;
                    finalXPosition = maxXPosition - centreX + additionalHorizontalOffset;
                    finalYPosition = maxYPosition - centreY + additionalVerticalOffset;
                    break;

                case ScrollItemPlacement.Right:
                    finalXPosition = minXPosition + additionalHorizontalOffset;
                    finalYPosition = previousYOffset + additionalVerticalOffset;
                    break;

                case ScrollItemPlacement.Bottom:
                    finalXPosition = previousXOffset + additionalHorizontalOffset;
                    finalYPosition = minYPosition + additionalVerticalOffset;
                    break;

                default:
                    finalXPosition = previousXOffset + additionalHorizontalOffset;
                    finalYPosition = previousYOffset + additionalVerticalOffset;
                    break;
            }
        }

        // Clamp to scrollable limits
        if (finalXPosition < 0) finalXPosition = 0;
        if (finalXPosition > scrollViewer.ScrollableWidth) finalXPosition = scrollViewer.ScrollableWidth;
        if (finalYPosition < 0) finalYPosition = 0;
        if (finalYPosition > scrollViewer.ScrollableHeight) finalYPosition = scrollViewer.ScrollableHeight;

        await scrollViewer.ChangeViewCoreAsync(finalXPosition, finalYPosition, zoomFactor: null, disableAnimation);
    }

    /// <summary>
    /// Changes the view of <see cref="ScrollViewer"/> asynchronously with animation support.
    /// Recovered from <c>sub_18007FC00</c> (<c>&lt;ChangeViewAsync&gt;d__24</c>).
    /// </summary>
    internal static async Task ChangeViewCoreAsync(
        this ScrollViewer scrollViewer,
        double? horizontalOffset,
        double? verticalOffset,
        float? zoomFactor,
        bool disableAnimation)
    {
        if (scrollViewer == null) return;

        if (horizontalOffset.HasValue)
        {
            if (horizontalOffset.Value > scrollViewer.ScrollableWidth)
                horizontalOffset = scrollViewer.ScrollableWidth;
            else if (horizontalOffset.Value < 0)
                horizontalOffset = 0;
        }

        if (verticalOffset.HasValue)
        {
            if (verticalOffset.Value > scrollViewer.ScrollableHeight)
                verticalOffset = scrollViewer.ScrollableHeight;
            else if (verticalOffset.Value < 0)
                verticalOffset = 0;
        }

        if (horizontalOffset.HasValue && Math.Abs(horizontalOffset.Value - scrollViewer.HorizontalOffset) < 0.5
            && verticalOffset.HasValue && Math.Abs(verticalOffset.Value - scrollViewer.VerticalOffset) < 0.5)
        {
            return;
        }

        var tcs = new TaskCompletionSource<object>();
        void ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate) return;
            tcs.TrySetResult(null);
        }

        try
        {
            scrollViewer.ViewChanged += ViewChanged;
            scrollViewer.ChangeView(horizontalOffset, verticalOffset, zoomFactor, disableAnimation);
            await tcs.Task;
        }
        finally
        {
            scrollViewer.ViewChanged -= ViewChanged;
        }
    }

    private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null) return null;
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T m) return m;
            var d = FindDescendant<T>(child);
            if (d != null) return d;
        }
        return null;
    }
}
