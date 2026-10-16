using Microsoft.UI.Xaml.Controls.Primitives;

namespace DevWinUI;

public enum ScrollItemPlacement
{
    Default = 0,
    Left = 1,
    Top = 2,
    Center = 3,
    Right = 4,
    Bottom = 5
}

public static partial class ListViewExtensions
{
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

        if (finalXPosition < 0) finalXPosition = 0;
        if (finalXPosition > scrollViewer.ScrollableWidth)
            finalXPosition = scrollViewer.ScrollableWidth;
        if (finalYPosition < 0) finalYPosition = 0;
        if (finalYPosition > scrollViewer.ScrollableHeight)
            finalYPosition = scrollViewer.ScrollableHeight;

        await scrollViewer.ChangeViewCoreAsync(finalXPosition, finalYPosition, zoomFactor: null, disableAnimation);
    }

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
