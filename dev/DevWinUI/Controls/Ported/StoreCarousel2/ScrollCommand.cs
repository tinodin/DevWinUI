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
/// <item><description>CanExecute <c>0x18150AF10</c> → <c>0x18150C860</c>:
/// Left <c>HorizontalOffset &gt; 0</c>; Right <c>ScrollableWidth &gt; HorizontalOffset</c>
/// (<c>0x1804FF9A0</c>). Attach <c>0x18150B1F0</c> also SizeChanged on content.</description></item>
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

    private FrameworkElement _contentSizeSource;

    private void AttachScrollViewer()
    {
        if (_scrollViewer == null)
        {
            return;
        }

        // 0x18150B1F0: ViewChanged + SizeChanged on ScrollViewer, and SizeChanged
        // on the content child (SV field +344 → UIElement cast unk_183DCA040).
        // Content SizeChanged is what refreshes CanExecute when the strip measures
        // before any scroll — without it, Right stays Disabled until ViewChanged.
        _scrollViewer.ViewChanged += OnScrollViewerViewChanged;
        _scrollViewer.SizeChanged += OnScrollViewerSizeChanged;
        _scrollViewer.Loaded += OnScrollViewerLoaded;
        AttachContentSizeSource(_scrollViewer.Content as FrameworkElement);
    }

    private void OnScrollViewerLoaded(object sender, RoutedEventArgs e)
    {
        // Template/content may appear after TargetElement is first set.
        if (_scrollViewer != null)
        {
            AttachContentSizeSource(_scrollViewer.Content as FrameworkElement);
            NotifyCanExecuteChanged();
        }
    }

    private void AttachContentSizeSource(FrameworkElement content)
    {
        if (_contentSizeSource != null)
        {
            _contentSizeSource.SizeChanged -= OnScrollViewerSizeChanged;
            _contentSizeSource = null;
        }

        if (content == null)
        {
            return;
        }

        _contentSizeSource = content;
        _contentSizeSource.SizeChanged += OnScrollViewerSizeChanged;
    }

    private void DetachScrollViewer()
    {
        if (_contentSizeSource != null)
        {
            _contentSizeSource.SizeChanged -= OnScrollViewerSizeChanged;
            _contentSizeSource = null;
        }

        if (_scrollViewer == null)
        {
            return;
        }

        _scrollViewer.ViewChanged -= OnScrollViewerViewChanged;
        _scrollViewer.SizeChanged -= OnScrollViewerSizeChanged;
        _scrollViewer.Loaded -= OnScrollViewerLoaded;
        _scrollViewer = null;
    }

    private void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e) => NotifyCanExecuteChanged();

    private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e) => NotifyCanExecuteChanged();

    /// <summary>0x18150C860 — getters <c>0x1804FF840</c> (HorizontalOffset),
    /// <c>0x1804FF9A0</c> (ScrollableWidth, vtable +336), <c>0x1804FFA50</c> /
    /// <c>0x1804FF8F0</c> (vertical pair).</summary>
    private static bool CanScroll(ScrollViewer sv, ScrollDirection direction)
    {
        switch (direction)
        {
            case ScrollDirection.Left:
                return sv.HorizontalOffset > 0.0;
            case ScrollDirection.Right:
                // ASM: ScrollableWidth > HorizontalOffset (not ExtentWidth-ViewportWidth-epsilon).
                return sv.ScrollableWidth > sv.HorizontalOffset;
            case ScrollDirection.Up:
                return sv.VerticalOffset > 0.0;
            case ScrollDirection.Down:
                return sv.ScrollableHeight > sv.VerticalOffset;
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
