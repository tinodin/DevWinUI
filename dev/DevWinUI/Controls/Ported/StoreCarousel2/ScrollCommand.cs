using Microsoft.UI.Xaml.Input;

namespace DevWinUI;

public partial class ScrollCommand : XamlUICommand
{
    private ScrollViewer _scrollViewer;
    private FrameworkElement _contentSizeSource;

    public ScrollCommand()
    {
        ExecuteRequested += OnExecuteRequested;
        CanExecuteRequested += OnCanExecuteRequested;
    }

    public static readonly DependencyProperty ScrollDirectionProperty =
        DependencyProperty.Register(
            nameof(ScrollDirection),
            typeof(ScrollDirection),
            typeof(ScrollCommand),
            new PropertyMetadata(ScrollDirection.None, OnScrollDirectionChanged));

    public ScrollDirection ScrollDirection
    {
        get => (ScrollDirection)GetValue(ScrollDirectionProperty);
        set => SetValue(ScrollDirectionProperty, value);
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

    public static readonly DependencyProperty TargetElementProperty =
        DependencyProperty.Register(
            nameof(TargetElement),
            typeof(FrameworkElement),
            typeof(ScrollCommand),
            new PropertyMetadata(null, OnTargetElementChanged));

    public FrameworkElement TargetElement
    {
        get => (FrameworkElement)GetValue(TargetElementProperty);
        set => SetValue(TargetElementProperty, value);
    }

    private static void OnTargetElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var cmd = (ScrollCommand)d;
        cmd.DetachScrollViewer();
        cmd._scrollViewer = ResolveScrollViewer(e.NewValue as FrameworkElement);
        cmd.AttachScrollViewer();
        cmd.NotifyCanExecuteChanged();
    }

    private void OnExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
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
        var sv = _scrollViewer ?? ResolveScrollViewer(TargetElement);
        args.CanExecute = sv != null && CanScroll(sv, ScrollDirection);
    }

    private void AttachScrollViewer()
    {
        if (_scrollViewer == null)
        {
            return;
        }

        _scrollViewer.ViewChanged += OnScrollViewerViewChanged;
        _scrollViewer.SizeChanged += OnScrollViewerSizeChanged;
        _scrollViewer.Loaded += OnScrollViewerLoaded;
        AttachContentSizeSource(_scrollViewer.Content as FrameworkElement);
    }

    private void OnScrollViewerLoaded(object sender, RoutedEventArgs e)
    {

        if (_scrollViewer != null)
        {
            AttachContentSizeSource(_scrollViewer.Content as FrameworkElement);
            NotifyCanExecuteChanged();
        }
    }

    private void AttachContentSizeSource(FrameworkElement content)
    {
        _contentSizeSource?.SizeChanged -= OnScrollViewerSizeChanged;
        _contentSizeSource = null;

        if (content == null)
        {
            return;
        }

        _contentSizeSource = content;
        _contentSizeSource.SizeChanged += OnScrollViewerSizeChanged;
    }

    private void DetachScrollViewer()
    {
        _contentSizeSource?.SizeChanged -= OnScrollViewerSizeChanged;
        _contentSizeSource = null;

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

    private static bool CanScroll(ScrollViewer sv, ScrollDirection direction)
    {
        return direction switch
        {
            ScrollDirection.Left => sv.HorizontalOffset > 0.0,
            ScrollDirection.Right => sv.ScrollableWidth > sv.HorizontalOffset,
            ScrollDirection.Up => sv.VerticalOffset > 0.0,
            ScrollDirection.Down => sv.ScrollableHeight > sv.VerticalOffset,
            _ => false,
        };
    }

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

        return element.FindDescendant<ScrollViewer>();
    }
}

public enum ScrollDirection
{
    None = 0,
    Left = 1,
    Right = 2,
    Up = 3,
    Down = 4
}
