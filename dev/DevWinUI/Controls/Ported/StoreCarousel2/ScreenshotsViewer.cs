using Microsoft.UI.Xaml.Input;

namespace DevWinUI;

public sealed partial class ScreenshotsViewer : UserControl
{
    public event EventHandler<object> ScreenshotClicked;
    public event EventHandler Clicked;

    public ListViewBase TileList => ScreenshotTileList;
    public int LastClickedIndex { get; private set; }

    public PreviewViewerHelper Helper { get; set; }

    public ScreenshotsViewer()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        AttachTileList();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachTileList();
        ApplyAgeRestriction(AgeRestricted);
    }

    private void ApplyAgeRestriction(bool ageRestricted)
    {
        if (ScreenshotTemplateSelector != null)
        {
            ScreenshotTemplateSelector.AgeRestricted = ageRestricted;
        }
    }

    private void AttachTileList()
    {
        if (ScreenshotTileList == null) return;

        ScreenshotTileList.ItemClick -= OnTileListClick;
        ScreenshotTileList.ItemClick += OnTileListClick;
        ScreenshotTileList.ContainerContentChanging -= OnContainerContentChanging;
        ScreenshotTileList.ContainerContentChanging += OnContainerContentChanging;
        ScreenshotTileList.Loaded -= OnTileListLoaded;
        ScreenshotTileList.Loaded += OnTileListLoaded;
        // In-strip keyboard Left/Right: SmoothScroll Center (Toolkit case 3 in sub_180080120).
        // Gated so PreviewViewer Escape restore (OnPreviewViewerClosed has no SmoothScroll [ASM 0x1821B94F0])
        // does not Center — only when focus moves between tiles already in this list.
        ScreenshotTileList.GettingFocus -= OnTileListGettingFocus;
        ScreenshotTileList.GettingFocus += OnTileListGettingFocus;
        ScreenshotTileList.PreviewKeyDown -= OnTileListPreviewKeyDown;
        ScreenshotTileList.PreviewKeyDown += OnTileListPreviewKeyDown;
        if (PreviewViewModel != null)
            ScreenshotTileList.ItemsSource = PreviewViewModel.Items;
        ApplyAgeRestriction(AgeRestricted);
        ScreenshotTileList.SelectedIndex = SelectedIndex;
        _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, ForceUpdateVideoThumbnails);
    }

    private void OnTileListGettingFocus(UIElement sender, GettingFocusEventArgs e)
    {
        if (e.InputDevice != FocusInputDeviceKind.Keyboard || ScreenshotTileList == null)
        {
            return;
        }

        // Store OnPreviewViewerClosed does not SmoothScroll. Focus restore into the strip from
        // outside (Escape / popup) must not Center — only in-strip keyboard tile-to-tile moves.
        if (e.OldFocusedElement is not DependencyObject oldHost ||
            FindAscendantOrSelf<ListViewItem>(oldHost) is not ListViewItem oldItem ||
            !IsDescendantOf(ScreenshotTileList, oldItem))
        {
            return;
        }

        if (e.NewFocusedElement is not FrameworkElement target)
        {
            return;
        }

        var item = FindAscendantOrSelf<ListViewItem>(target);
        if (item == null)
        {
            return;
        }

        var index = ScreenshotTileList.IndexFromContainer(item);
        if (index >= 0)
        {
            // SmoothScroll Center + animated: Toolkit case 3 in sub_180080120 (disableAnimation=false).
            _ = ScreenshotTileList.SmoothScrollIntoViewWithIndexAsync(index, ScrollItemPlacement.Center, disableAnimation: false, scrollIfVisible: true);
        }
    }

    private void OnTileListPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Left && e.Key != Windows.System.VirtualKey.Right)
        {
            return;
        }

        if (ScreenshotTileList?.XamlRoot == null || ScreenshotTileList.Items.Count == 0)
        {
            return;
        }

        var currentFocused = FocusManager.GetFocusedElement(ScreenshotTileList.XamlRoot) as DependencyObject;
        var currentItem = FindAscendantOrSelf<ListViewItem>(currentFocused);
        if (currentItem != null)
        {
            var currentIndex = ScreenshotTileList.IndexFromContainer(currentItem);
            if (currentIndex >= 0)
            {
                int targetIndex = e.Key == Windows.System.VirtualKey.Left ? currentIndex - 1 : currentIndex + 1;
                if (targetIndex >= 0 && targetIndex < ScreenshotTileList.Items.Count)
                {
                    e.Handled = true;
                    // Focus only — Center comes from GettingFocus when OldFocused is in-strip.
                    FocusItem(targetIndex, FocusState.Keyboard);
                }
            }
        }
    }

    private void OnTileListLoaded(object sender, RoutedEventArgs e) => ForceUpdateVideoThumbnails();

    private void ForceUpdateVideoThumbnails()
    {
        if (ScreenshotTileList == null) return;
        for (int i = 0; i < ScreenshotTileList.Items.Count; i++)
        {
            if (ScreenshotTileList.Items[i] is not VideoPlayerSource videoItem || videoItem.ImageUri == null) continue;
            var container = ScreenshotTileList.ContainerFromIndex(i) as FrameworkElement;
            if (container == null) continue;
            DependencyObject searchRoot = container;
            if (container is ListViewItem lvi && lvi.ContentTemplateRoot is DependencyObject lviRoot)
                searchRoot = lviRoot;
            var image = FindDescendant<Image>(searchRoot) ?? FindDescendant<Image>(container);
            if (image == null) continue;
            if (image.Source is BitmapImage existingBmp && existingBmp.UriSource?.ToString() == videoItem.ImageUri.ToString())
                continue;
            try
            {
                image.Source = new BitmapImage(videoItem.ImageUri) { CreateOptions = BitmapCreateOptions.None, DecodePixelHeight = 316 };
            }
            catch { }
            var behaviors = Microsoft.Xaml.Interactivity.Interaction.GetBehaviors(image);
            foreach (var b in behaviors)
            {
                if (b is ViewDisplayedBehavior vdb && vdb.ViewDisplayedAction == null)
                {
                    var capturedImage = image;
                    var capturedUri = videoItem.ImageUri;
                    vdb.ViewDisplayedAction = () =>
                    {
                        if (capturedImage.Source != null) return;
                        try { capturedImage.Source = new BitmapImage(capturedUri) { CreateOptions = BitmapCreateOptions.None, DecodePixelHeight = 316 }; } catch { }
                    };
                    break;
                }
            }
        }
    }

    private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue) return;
        if (args.Item is not VideoPlayerSource videoItem || videoItem.ImageUri == null) return;

        if (args.Phase == 0)
        {
            args.RegisterUpdateCallback(OnContainerContentChanging);
            return;
        }

        var container = args.ItemContainer as FrameworkElement;
        if (container == null)
        {
            if (args.Phase < 3) args.RegisterUpdateCallback(OnContainerContentChanging);
            return;
        }

        DependencyObject searchRoot = container;
        if (args.ItemContainer is ListViewItem lvi && lvi.ContentTemplateRoot is DependencyObject lviRoot)
            searchRoot = lviRoot;

        var image = FindDescendant<Image>(searchRoot) ?? FindDescendant<Image>(container);
        if (image == null)
        {
            if (args.Phase < 3) args.RegisterUpdateCallback(OnContainerContentChanging);
            return;
        }

        var behaviors = Microsoft.Xaml.Interactivity.Interaction.GetBehaviors(image);
        ViewDisplayedBehavior targetVdb = null;
        foreach (var b in behaviors)
            if (b is ViewDisplayedBehavior vdb) { targetVdb = vdb; break; }

        bool needsLoad = true;
        if (image.Source is BitmapImage existingBmp && existingBmp.UriSource?.ToString() == videoItem.ImageUri.ToString())
            needsLoad = false;

        if (needsLoad)
        {
            try { image.Source = new BitmapImage(videoItem.ImageUri) { CreateOptions = BitmapCreateOptions.None, DecodePixelHeight = 316 }; } catch { }
        }

        if (targetVdb != null && targetVdb.ViewDisplayedAction == null)
        {
            var capturedImage = image;
            var capturedUri = videoItem.ImageUri;
            targetVdb.ViewDisplayedAction = () =>
            {
                if (capturedImage.Source != null) return;
                try { capturedImage.Source = new BitmapImage(capturedUri) { CreateOptions = BitmapCreateOptions.None, DecodePixelHeight = 316 }; } catch { }
            };
        }
    }

    private void OnTileListClick(object sender, ItemClickEventArgs e)
    {
        // sub_1821817F0 [ASM/Hex-Rays]: raise Clicked, then
        // SetUpPreviewViewer(helper, ClickedItem, flag: 1).
        var index = ScreenshotTileList.Items.IndexOf(e.ClickedItem);
        if (index < 0) index = 0;
        LastClickedIndex = index;
        ScreenshotClicked?.Invoke(this, e.ClickedItem ?? index);
        Clicked?.Invoke(this, EventArgs.Empty);

        if (Helper != null && e.ClickedItem != null)
        {
            Helper.SetUpPreviewViewer(e.ClickedItem, flag: true);
        }
        else if (Helper?.ListViewSource != null)
        {
            Helper.ListViewSource.ScrollToIndex(index);
        }
    }

    internal void OnPreviewViewModelChanged(PreviewModuleViewModel oldValue, PreviewModuleViewModel newValue)
    {
        if (ScreenshotTileList != null && newValue != null)
            ScreenshotTileList.ItemsSource = newValue.Items;
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        var isEmpty = ScreenshotTileList == null || ScreenshotTileList.Items.Count == 0;
        if (isEmpty) { }
    }

    public void BringItemFlushToEdge(int index)
    {
        if (ScreenshotTileList == null || index < 0 || index >= ScreenshotTileList.Items.Count) return;
        if (IsItemFullyVisible(index)) return;
        ScreenshotTileList.ScrollIntoView(ScreenshotTileList.Items[index], ScrollIntoViewAlignment.Leading);
        _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => FlushContainerToEdge(index));
    }

    /// <summary>
    /// Focus a strip tile. Port helper name — Store has no <c>FocusItem</c> string/method.
    /// Does not SmoothScroll here; in-strip keyboard Center is handled by <see cref="OnTileListGettingFocus"/>.
    /// Close/sync scroll uses <see cref="PreviewViewerHelperListViewSource.ScrollToIndex"/>
    /// → UpdateSelectedIndex (Default, disableAnimation: true) [ASM 0x1821B9980].
    /// </summary>
    public void FocusItem(int index, FocusState state)
    {
        if (ScreenshotTileList == null || index < 0 || index >= ScreenshotTileList.Items.Count) return;

        try
        {
            if (ScreenshotTileList.ContainerFromIndex(index) is Control c)
            {
                c.Focus(state);
                return;
            }
        }
        catch { }

        _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            try
            {
                if (ScreenshotTileList.ContainerFromIndex(index) is Control d)
                {
                    d.Focus(state);
                }
            }
            catch { }
        });
    }

    private void FlushContainerToEdge(int index)
    {
        var sv = FindDescendant<ScrollViewer>(ScreenshotTileList);
        FrameworkElement container = null;
        try { container = ScreenshotTileList.ContainerFromIndex(index) as FrameworkElement; } catch { return; }
        if (sv == null || container == null) return;
        var content = sv.Content as UIElement; if (content == null) return;
        var origin = container.TransformToVisual(content).TransformPoint(new Windows.Foundation.Point(0, 0));
        var target = origin.X; if (target < 0) target = 0; if (target > sv.ScrollableWidth) target = sv.ScrollableWidth;
        sv.ChangeView(target, null, null, true);
    }

    private bool IsItemFullyVisible(int index)
    {
        var sv = FindDescendant<ScrollViewer>(ScreenshotTileList);
        FrameworkElement container = null;
        try { container = ScreenshotTileList.ContainerFromIndex(index) as FrameworkElement; } catch { return false; }
        if (sv == null || container == null) return false;
        var origin = container.TransformToVisual(sv).TransformPoint(new Windows.Foundation.Point(0, 0));
        var right = origin.X + container.ActualWidth;
        const double epsilon = 1.0;
        return origin.X >= -epsilon && right <= sv.ViewportWidth + epsilon;
    }

    private ScreenshotDataTemplateSelector ScreenshotTemplateSelector =>
        Resources["ScreenshotTemplateSelector"] as ScreenshotDataTemplateSelector;

    private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null) return null;
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T m) return m;
            var d = FindDescendant<T>(child);
            if (d != null) return d;
        }
        return null;
    }

    private static T FindAscendantOrSelf<T>(DependencyObject element) where T : DependencyObject
    {
        while (element != null)
        {
            if (element is T match) return match;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }

    private static bool IsDescendantOf(DependencyObject root, DependencyObject node)
    {
        while (node != null)
        {
            if (node == root) return true;
            node = VisualTreeHelper.GetParent(node);
        }
        return false;
    }
}
