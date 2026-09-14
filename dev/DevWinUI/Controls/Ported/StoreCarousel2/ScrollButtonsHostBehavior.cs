using Microsoft.Xaml.Interactivity;

namespace DevWinUI;

/// <summary>
/// Hosts Left/Right scroll buttons for a horizontal strip.
/// Store XAML assigns <c>ScrollCommand</c> on each button; this behavior sets
/// <c>TargetElement</c> to the strip <see cref="ScrollViewer"/> (<c>0x18150B100</c>)
/// and toggles button visibility on pointer-over / scrollability.
/// </summary>
public sealed partial class ScrollButtonsHostBehavior : Behavior<Grid>
{
    public static readonly DependencyProperty LeftScrollButtonProperty =
        DependencyProperty.Register(nameof(LeftScrollButton), typeof(Button), typeof(ScrollButtonsHostBehavior),
            new PropertyMetadata(null, OnScrollButtonChanged));

    public static readonly DependencyProperty RightScrollButtonProperty =
        DependencyProperty.Register(nameof(RightScrollButton), typeof(Button), typeof(ScrollButtonsHostBehavior),
            new PropertyMetadata(null, OnScrollButtonChanged));

    public Button LeftScrollButton
    {
        get => (Button)GetValue(LeftScrollButtonProperty);
        set => SetValue(LeftScrollButtonProperty, value);
    }

    public Button RightScrollButton
    {
        get => (Button)GetValue(RightScrollButtonProperty);
        set => SetValue(RightScrollButtonProperty, value);
    }

    private ScrollViewer _scrollViewer;
    private bool _isPointerOver;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.Unloaded += OnUnloaded;
        AssociatedObject.PointerEntered += OnPointerEntered;
        AssociatedObject.PointerExited += OnPointerExited;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.Loaded -= OnLoaded;
        AssociatedObject.Unloaded -= OnUnloaded;
        AssociatedObject.PointerEntered -= OnPointerEntered;
        AssociatedObject.PointerExited -= OnPointerExited;
        DetachScrollViewer();
    }

    private static void OnScrollButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = (ScrollButtonsHostBehavior)d;
        if (behavior._scrollViewer != null)
        {
            behavior.WireScrollCommandTargets(behavior._scrollViewer);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachScrollViewer();
        UpdateButtons();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachScrollViewer();
    }

    private void OnPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isPointerOver = true;
        UpdateButtons();
    }

    private void OnPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isPointerOver = false;
        UpdateButtons();
    }

    private void AttachScrollViewer()
    {
        DetachScrollViewer();
        _scrollViewer = FindDescendant<ScrollViewer>(AssociatedObject);
        if (_scrollViewer != null)
        {
            _scrollViewer.ViewChanged += OnViewChanged;
            _scrollViewer.SizeChanged += OnSizeChanged;
            AssociatedObject.SizeChanged += OnSizeChanged;
            WireScrollCommandTargets(_scrollViewer);
            _ = AssociatedObject.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, UpdateButtons);
        }
    }

    private void WireScrollCommandTargets(ScrollViewer scrollViewer)
    {
        // Store connect sets ScrollCommand.TargetElement to the strip ScrollViewer.
        if (LeftScrollButton?.Command is ScrollCommand left)
        {
            left.TargetElement = scrollViewer;
        }

        if (RightScrollButton?.Command is ScrollCommand right)
        {
            right.TargetElement = scrollViewer;
        }
    }

    private void DetachScrollViewer()
    {
        if (_scrollViewer != null)
        {
            _scrollViewer.ViewChanged -= OnViewChanged;
            _scrollViewer.SizeChanged -= OnSizeChanged;
            _scrollViewer = null;
        }
        if (AssociatedObject != null)
        {
            AssociatedObject.SizeChanged -= OnSizeChanged;
        }
    }

    private void OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e) => UpdateButtons();

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateButtons();

    private void UpdateButtons()
    {
        var left = LeftScrollButton;
        var right = RightScrollButton;
        if (left == null || right == null) return;
        var sv = _scrollViewer ?? FindDescendant<ScrollViewer>(AssociatedObject);
        if (sv == null) return;

        WireScrollCommandTargets(sv);

        if (!_isPointerOver)
        {
            left.Visibility = Visibility.Collapsed;
            right.Visibility = Visibility.Collapsed;
            return;
        }

        var canScroll = sv.ExtentWidth > sv.ViewportWidth + 1;
        if (!canScroll)
        {
            left.Visibility = Visibility.Collapsed;
            right.Visibility = Visibility.Collapsed;
            return;
        }

        left.Visibility = sv.HorizontalOffset > 1 ? Visibility.Visible : Visibility.Collapsed;
        right.Visibility = sv.HorizontalOffset < sv.ExtentWidth - sv.ViewportWidth - 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null) return null;
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            var desc = FindDescendant<T>(child);
            if (desc != null) return desc;
        }
        return null;
    }
}
