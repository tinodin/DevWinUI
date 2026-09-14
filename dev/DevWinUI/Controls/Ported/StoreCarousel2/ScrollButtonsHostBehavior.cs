using Microsoft.Xaml.Interactivity;

namespace DevWinUI;

/// <summary>
/// Hosts Left/Right scroll buttons for a horizontal strip.
/// Store XAML assigns <c>ScrollCommand</c> on each button; this behavior sets
/// <c>TargetElement</c> to the strip <see cref="ScrollViewer"/> (<c>0x18150B100</c>)
/// and toggles button <see cref="UIElement.Visibility"/> on pointer-over.
/// </summary>
/// <remarks>
/// Store proof for show/hide (not invented offset math):
/// <list type="bullet">
/// <item><description>ScreenshotsViewer.xaml: both buttons start <c>Collapsed</c>;
/// <c>ScrollButtonsHostBehavior</c> is on the strip Grid.</description></item>
/// <item><description>ButtonResourceDictionary: Disabled visual sets Root
/// <c>Visibility</c> to <c>ArrowScrollViewerButtonDisabledVisiblity</c> (=
/// <c>Collapsed</c>). Edge hide is therefore <c>ScrollCommand.CanExecute</c>
/// → <c>Button.IsEnabled</c> → Disabled state — not Host <c>Visibility</c>
/// math.</description></item>
/// <item><description>CanExecute <c>0x18150C860</c>: Left if
/// <c>HorizontalOffset &gt; 0</c>; Right if
/// <c>ScrollableWidth &gt; HorizontalOffset</c> (<c>0x1804FF9A0</c>).</description></item>
/// <item><description>Host method bodies sit in R2R merge regions (not
/// decompiled). Contract from XAML + CanExecute + Disabled style: on pointer
/// over set both buttons <c>Visible</c>; on exit <c>Collapsed</c>. Do not
/// gate Host Visibility on ExtentWidth (that blocked hover before the first
/// scroll when only ViewChanged refreshed the invented check).</description></item>
/// </list>
/// </remarks>
public sealed partial class ScrollButtonsHostBehavior : Behavior<Grid>
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(ScrollButtonsHostBehavior),
            new PropertyMetadata(true, OnIsEnabledChanged));

    public static readonly DependencyProperty LeftScrollButtonProperty =
        DependencyProperty.Register(nameof(LeftScrollButton), typeof(Button), typeof(ScrollButtonsHostBehavior),
            new PropertyMetadata(null, OnScrollButtonChanged));

    public static readonly DependencyProperty RightScrollButtonProperty =
        DependencyProperty.Register(nameof(RightScrollButton), typeof(Button), typeof(ScrollButtonsHostBehavior),
            new PropertyMetadata(null, OnScrollButtonChanged));

    /// <summary>Store DP <c>ScrollButtonsHostBehavior.IsEnabled</c> (Boolean, register <c>0x180D248FE</c>).</summary>
    public bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

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

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ScrollButtonsHostBehavior)d).UpdateButtons();
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
            // ViewChanged keeps CanExecute current after scroll; Host only needs
            // TargetElement wiring here (visibility is pointer-over only).
            _scrollViewer.ViewChanged += OnViewChanged;
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
            _scrollViewer = null;
        }
    }

    private void OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        // CanExecute refresh is owned by ScrollCommand attach (0x18150B1F0).
        // Re-apply pointer-over visibility if the strip was still hovered.
        if (_isPointerOver)
        {
            UpdateButtons();
        }
    }

    private void UpdateButtons()
    {
        var left = LeftScrollButton;
        var right = RightScrollButton;
        if (left == null || right == null)
        {
            return;
        }

        var sv = _scrollViewer ?? FindDescendant<ScrollViewer>(AssociatedObject);
        if (sv != null)
        {
            WireScrollCommandTargets(sv);
        }

        // Store: Host Visibility is pointer-over (+ IsEnabled DP). Scrollability
        // hides an edge via CanExecute → Disabled → ArrowScrollViewerButtonDisabledVisiblity.
        if (!_isPointerOver || !IsEnabled)
        {
            left.Visibility = Visibility.Collapsed;
            right.Visibility = Visibility.Collapsed;
            return;
        }

        left.Visibility = Visibility.Visible;
        right.Visibility = Visibility.Visible;
    }

    private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null)
        {
            return null;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            var desc = FindDescendant<T>(child);
            if (desc != null)
            {
                return desc;
            }
        }

        return null;
    }
}
