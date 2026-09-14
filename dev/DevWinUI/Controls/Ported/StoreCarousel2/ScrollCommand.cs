using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace DevWinUI;

/// <summary>
/// Scrolls a target <see cref="ScrollViewer"/> by direction.
/// Ported from <c>WinStore.UX.Common.ScrollCommand</c> (Hex-Rays / EEType).
/// </summary>
/// <remarks>
/// Store uses a custom <c>DependencyObject</c> + <c>ICommand</c> assigned as a
/// <c>Button.Command</c> property element. WinUI 3 only accepts WinRT command
/// types that way, so this port derives from <see cref="XamlUICommand"/> (the
/// WinUI 3-compatible base) while keeping Store Execute/CanExecute behavior.
/// <list type="bullet">
/// <item><description>Execute <c>0x18150AF40</c> → <c>0x18150C930</c>:
/// <c>ChangeView(offset ± ViewportWidth*0.9, …, disableAnimation: false)</c>.</description></item>
/// <item><description>CanExecute <c>0x18150AF10</c> → <c>0x18150C860</c>.</description></item>
/// <item><description>Enum: Left=1, Right=2, Up=3, Down=4.</description></item>
/// </list>
/// </remarks>
public sealed class ScrollCommand : XamlUICommand
{
    private ScrollViewer _scrollViewer;

    public ScrollCommand()
    {
        ExecuteRequested += OnExecuteRequested;
        CanExecuteRequested += OnCanExecuteRequested;
    }

    /// <summary>Direction to scroll. Store enum: Left=1, Right=2 (XAML "Left"/"Right").</summary>
    public ScrollDirection ScrollDirection
    {
        get => (ScrollDirection)GetValue(ScrollDirectionProperty);
        set => SetValue(ScrollDirectionProperty, value);
    }

    public static readonly DependencyProperty ScrollDirectionProperty =
        DependencyProperty.Register(
            nameof(ScrollDirection),
            typeof(ScrollDirection),
            typeof(ScrollCommand),
            new PropertyMetadata(ScrollDirection.None, OnScrollDirectionChanged));

    /// <summary>
    /// Scroll target. Store resolves this to a <see cref="ScrollViewer"/>
    /// (<c>0x18150B170</c>) and caches it for Execute/CanExecute.
    /// </summary>
    public FrameworkElement TargetElement
    {
        get => (FrameworkElement)GetValue(TargetElementProperty);
        set => SetValue(TargetElementProperty, value);
    }

    public static readonly DependencyProperty TargetElementProperty =
        DependencyProperty.Register(
            nameof(TargetElement),
            typeof(FrameworkElement),
            typeof(ScrollCommand),
            new PropertyMetadata(null, OnTargetElementChanged));

    private void OnExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        // 0x18150AF40 → 0x18150C840 → 0x18150C930 / 0x18150CA30
        var sv = _scrollViewer ?? ResolveScrollViewer(TargetElement);
        if (sv == null)
        {
            return;
        }

        ScrollByDirection(sv, ScrollDirection);
        NotifyCanExecuteChanged();
    }

    private void OnCanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
    {
        // 0x18150AF10 → 0x18150C860
        var sv = _scrollViewer ?? ResolveScrollViewer(TargetElement);
        args.CanExecute = sv != null && CanScroll(sv, ScrollDirection);
    }

    private static void OnScrollDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var cmd = (ScrollCommand)d;
        if (cmd._scrollViewer != null)
        {
            ScrollByDirection(cmd._scrollViewer, (ScrollDirection)e.NewValue);
        }

        cmd.NotifyCanExecuteChanged();
    }

    private static void OnTargetElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // 0x18150B100 / 0x18150B3E0: resolve TargetElement → ScrollViewer, attach, cache.
        var cmd = (ScrollCommand)d;
        cmd.DetachScrollViewer();
        cmd._scrollViewer = ResolveScrollViewer(e.NewValue as FrameworkElement);
        cmd.AttachScrollViewer();
        cmd.NotifyCanExecuteChanged();
    }

    private void AttachScrollViewer()
    {
        if (_scrollViewer == null)
        {
            return;
        }

        // 0x18150B1F0 attaches view/size listeners so CanExecute stays current.
        _scrollViewer.ViewChanged += OnScrollViewerViewChanged;
        _scrollViewer.SizeChanged += OnScrollViewerSizeChanged;
    }

    private void DetachScrollViewer()
    {
        if (_scrollViewer == null)
        {
            return;
        }

        _scrollViewer.ViewChanged -= OnScrollViewerViewChanged;
        _scrollViewer.SizeChanged -= OnScrollViewerSizeChanged;
        _scrollViewer = null;
    }

    private void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e) => NotifyCanExecuteChanged();

    private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e) => NotifyCanExecuteChanged();

    /// <summary>0x18150C860</summary>
    private static bool CanScroll(ScrollViewer sv, ScrollDirection direction)
    {
        const double epsilon = 0.0;
        switch (direction)
        {
            case ScrollDirection.Left:
                return sv.HorizontalOffset > epsilon;
            case ScrollDirection.Right:
                return sv.HorizontalOffset < sv.ExtentWidth - sv.ViewportWidth - epsilon;
            case ScrollDirection.Up:
                return sv.VerticalOffset > epsilon;
            case ScrollDirection.Down:
                return sv.VerticalOffset < sv.ExtentHeight - sv.ViewportHeight - epsilon;
            default:
                return false;
        }
    }

    /// <summary>0x18150C840 → 0x18150C930 / 0x18150CA30</summary>
    private static void ScrollByDirection(ScrollViewer sv, ScrollDirection direction)
    {
        switch (direction)
        {
            case ScrollDirection.Left:
            {
                var target = sv.HorizontalOffset - sv.ViewportWidth * 0.9;
                sv.ChangeView(target, null, null, disableAnimation: false);
                break;
            }
            case ScrollDirection.Right:
            {
                var target = sv.HorizontalOffset + sv.ViewportWidth * 0.9;
                sv.ChangeView(target, null, null, disableAnimation: false);
                break;
            }
            case ScrollDirection.Up:
            {
                var target = sv.VerticalOffset - sv.ViewportHeight * 0.9;
                sv.ChangeView(null, target, null, disableAnimation: false);
                break;
            }
            case ScrollDirection.Down:
            {
                var target = sv.VerticalOffset + sv.ViewportHeight * 0.9;
                sv.ChangeView(null, target, null, disableAnimation: false);
                break;
            }
        }
    }

    /// <summary>0x18150B170 — TargetElement must be (or resolve to) a ScrollViewer.</summary>
    private static ScrollViewer ResolveScrollViewer(FrameworkElement element)
    {
        if (element == null)
        {
            return null;
        }

        if (element is ScrollViewer sv)
        {
            return sv;
        }

        return FindDescendant<ScrollViewer>(element);
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

/// <summary>
/// <c>WinStore.UX.Common.ScrollDirection</c>. Values from <c>0x18150C860</c>/<c>0x18150C930</c>
/// switch cases (XAML names Left/Right confirmed).
/// </summary>
public enum ScrollDirection
{
    None = 0,
    Left = 1,
    Right = 2,
    Up = 3,
    Down = 4
}
