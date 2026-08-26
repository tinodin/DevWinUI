using System.Collections;

namespace DevWinUI;

public sealed partial class ScreenshotsViewer : UserControl
{
    public event EventHandler<object> ScreenshotClicked;
    public event EventHandler Clicked;

    public ListViewBase TileList => ScreenshotTileList;
    public int LastClickedIndex { get; private set; }

    public ScreenshotsViewer()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>
    /// Wires the <see cref="ScreenshotTileList"/> item-click handling, mirroring the
    /// Store's <c>ScreenshotsViewer.OnApplyTemplate</c> (GetTemplateChild("ScreenshotTileList")).
    /// </summary>
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
            // The Store changes the selector state without tearing down the
            // ItemsSource. Replacing it with null creates a short input-dead
            // window during rapid popup open/close cycles.
        }
    }

    private void AttachTileList()
    {
        if (ScreenshotTileList == null)
        {
            return;
        }

        ScreenshotTileList.ItemClick -= OnTileListClick;
        ScreenshotTileList.ItemClick += OnTileListClick;
        if (PreviewViewModel != null)
            ScreenshotTileList.ItemsSource = PreviewViewModel.Items;
        ApplyAgeRestriction(AgeRestricted);
        ScreenshotTileList.SelectedIndex = SelectedIndex;
    }

    private void OnTileListClick(object sender, ItemClickEventArgs e)
    {
        var index = ScreenshotTileList.Items.IndexOf(e.ClickedItem);
        if (index < 0) index = 0;
        LastClickedIndex = index;
        ScreenshotClicked?.Invoke(this, index);
        Clicked?.Invoke(this, EventArgs.Empty);
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
        if (isEmpty)
        {
        }
    }

    public void BringItemFlushToEdge(int index)
    {
        if (ScreenshotTileList == null || index < 0 || index >= ScreenshotTileList.Items.Count) return;
        if (IsItemFullyVisible(index)) return;
        ScreenshotTileList.ScrollIntoView(ScreenshotTileList.Items[index], ScrollIntoViewAlignment.Leading);
        _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => FlushContainerToEdge(index));
    }

    public void FocusItem(int index, FocusState state)
    {
        if (ScreenshotTileList == null || index < 0 || index >= ScreenshotTileList.Items.Count) return;
        try { if (ScreenshotTileList.ContainerFromIndex(index) is Control c) { c.Focus(state); return; } } catch { }
        BringItemFlushToEdge(index);
        _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { try { if (ScreenshotTileList.ContainerFromIndex(index) is Control d) d.Focus(state); } catch { } });
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
}
