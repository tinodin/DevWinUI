using Microsoft.UI.Xaml.Input;

namespace DevWinUI;

public sealed partial class ScreenshotsViewer : UserControl
{
    public event EventHandler<object> ScreenshotClicked;
    public event EventHandler Clicked;
    public event EventHandler SignInClicked;

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
        ApplyAgeRestriction();
    }

    private void ApplyAgeRestriction()
    {
        var selector = ScreenshotTemplateSelector;
        if (selector != null)
        {
            selector.AgeRestricted = AgeRestricted;
            selector.IsAgeBlocked = IsAgeBlocked;
        }

        RefreshTileListSource();
        _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, HookAgeGateSignInButtons);
    }

    private void RefreshTileListSource()
    {
        if (ScreenshotTileList == null)
        {
            return;
        }

        var items = PreviewViewModel?.Items;
        if (items == null)
        {
            ScreenshotTileList.ItemsSource = null;
            return;
        }

        if (AgeRestricted && items.Count > 0)
        {
            ScreenshotTileList.ItemsSource = new List<object> { items[0] };
            return;
        }

        ScreenshotTileList.ItemsSource = items;
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

        ScreenshotTileList.GettingFocus -= OnTileListGettingFocus;
        ScreenshotTileList.GettingFocus += OnTileListGettingFocus;
        ScreenshotTileList.PreviewKeyDown -= OnTileListPreviewKeyDown;
        ScreenshotTileList.PreviewKeyDown += OnTileListPreviewKeyDown;
        ApplyAgeRestriction();
        ScreenshotTileList.SelectedIndex = SelectedIndex;
        _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, ForceUpdateVideoThumbnails);
    }

    private void OnTileListGettingFocus(UIElement sender, GettingFocusEventArgs e)
    {
        if (e.InputDevice != FocusInputDeviceKind.Keyboard || ScreenshotTileList == null)
        {
            return;
        }

        if (e.OldFocusedElement is not DependencyObject oldHost || FindAscendantOrSelf<ListViewItem>(oldHost) is not ListViewItem oldItem || !IsDescendantOf(ScreenshotTileList, oldItem))
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

                    FocusItem(targetIndex, FocusState.Keyboard);
                }
            }
        }
    }

    private void OnTileListLoaded(object sender, RoutedEventArgs e)
    {
        ForceUpdateVideoThumbnails();
        HookAgeGateSignInButtons();
    }

    private void ForceUpdateVideoThumbnails()
    {
        if (ScreenshotTileList == null) return;
        for (int i = 0; i < ScreenshotTileList.Items.Count; i++)
        {
            if (ScreenshotTileList.Items[i] is not VideoPlayerSource videoItem || videoItem.ImageUri == null)
                continue;
            var container = ScreenshotTileList.ContainerFromIndex(i) as FrameworkElement;
            if (container == null)
                continue;
            DependencyObject searchRoot = container;
            if (container is ListViewItem lvi && lvi.ContentTemplateRoot is DependencyObject lviRoot)
                searchRoot = lviRoot;
            var image = FindDescendant<Image>(searchRoot) ?? FindDescendant<Image>(container);
            if (image == null)
                continue;
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
                        if (capturedImage.Source != null)
                            return;
                        try
                        {
                            capturedImage.Source = new BitmapImage(capturedUri) { CreateOptions = BitmapCreateOptions.None, DecodePixelHeight = 316 };
                        }
                        catch { }
                    };
                    break;
                }
            }
        }
    }

    private bool HookAgeGateSignInButton(FrameworkElement container)
    {
        if (container == null)
        {
            return false;
        }

        DependencyObject searchRoot = container;
        if (container is ListViewItem lvi && lvi.ContentTemplateRoot is DependencyObject lviRoot)
            searchRoot = lviRoot;

        var button = FindDescendant<Button>(searchRoot) ?? FindDescendant<Button>(container);
        if (button == null)
        {
            return false;
        }

        button.Click -= OnAgeGatingSignInClicked;
        button.Click += OnAgeGatingSignInClicked;
        return true;
    }

    private void HookAgeGateSignInButtons()
    {
        if (!AgeRestricted || ScreenshotTileList == null)
        {
            return;
        }

        for (var i = 0; i < ScreenshotTileList.Items.Count; i++)
        {
            if (ScreenshotTileList.ContainerFromIndex(i) is FrameworkElement container)
            {
                HookAgeGateSignInButton(container);
            }
        }
    }

    private void OnAgeGatingSignInClicked(object sender, RoutedEventArgs e) => SignInClicked?.Invoke(this, EventArgs.Empty);

    private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue) return;

        if (AgeRestricted && !HookAgeGateSignInButton(args.ItemContainer as FrameworkElement) && args.Phase < 3)
        {
            args.RegisterUpdateCallback(OnContainerContentChanging);
            return;
        }

        if (args.Item is not VideoPlayerSource videoItem || videoItem.ImageUri == null) return;

        if (args.Phase == 0)
        {
            args.RegisterUpdateCallback(OnContainerContentChanging);
            return;
        }

        var container = args.ItemContainer as FrameworkElement;
        if (container == null)
        {
            if (args.Phase < 3)
                args.RegisterUpdateCallback(OnContainerContentChanging);
            return;
        }

        DependencyObject searchRoot = container;
        if (args.ItemContainer is ListViewItem lvi && lvi.ContentTemplateRoot is DependencyObject lviRoot)
            searchRoot = lviRoot;

        var image = FindDescendant<Image>(searchRoot) ?? FindDescendant<Image>(container);
        if (image == null)
        {
            if (args.Phase < 3)
                args.RegisterUpdateCallback(OnContainerContentChanging);
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
            try
            {
                image.Source = new BitmapImage(videoItem.ImageUri) { CreateOptions = BitmapCreateOptions.None, DecodePixelHeight = 316 };
            }
            catch { }
        }

        if (targetVdb != null && targetVdb.ViewDisplayedAction == null)
        {
            var capturedImage = image;
            var capturedUri = videoItem.ImageUri;
            targetVdb.ViewDisplayedAction = () =>
            {
                if (capturedImage.Source != null)
                    return;
                try
                {
                    capturedImage.Source = new BitmapImage(capturedUri) { CreateOptions = BitmapCreateOptions.None, DecodePixelHeight = 316 };
                }
                catch { }
            };
        }
    }

    private void OnTileListClick(object sender, ItemClickEventArgs e)
    {
        var index = ScreenshotTileList.Items.IndexOf(e.ClickedItem);
        if (index < 0) index = 0;
        LastClickedIndex = index;
        ScreenshotClicked?.Invoke(this, e.ClickedItem ?? index);
        Clicked?.Invoke(this, EventArgs.Empty);

        if (AgeRestricted)
        {
            return;
        }

        if (Helper != null && e.ClickedItem != null)
        {
            Helper.SetUpPreviewViewer(e.ClickedItem);
        }
        else if (Helper?.ListViewSource != null)
        {
            Helper.ListViewSource.ScrollToIndex(index);
        }
    }

    internal void OnPreviewViewModelChanged(PreviewModuleViewModel oldValue, PreviewModuleViewModel newValue)
    {
        RefreshTileListSource();
    }

    public void BringItemFlushToEdge(int index)
    {
        if (ScreenshotTileList == null || index < 0 || index >= ScreenshotTileList.Items.Count)
            return;

        if (Helper?.Popup?.List != null && Helper.Popup.List.SelectedIndex == index)
            return;

        if (IsItemFullyVisible(index))
            return;
        ScreenshotTileList.ScrollIntoView(ScreenshotTileList.Items[index], ScrollIntoViewAlignment.Leading);
        _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => FlushContainerToEdge(index));
    }

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
