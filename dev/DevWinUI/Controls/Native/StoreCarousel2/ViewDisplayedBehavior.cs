using Microsoft.Xaml.Interactivity;

namespace DevWinUI;

public partial class ViewDisplayedBehavior : Behavior<FrameworkElement>
{
    public static readonly DependencyProperty ViewDisplayedActionProperty =
        DependencyProperty.Register(nameof(ViewDisplayedAction), typeof(Action), typeof(ViewDisplayedBehavior), new PropertyMetadata(null));

    public Action ViewDisplayedAction
    {
        get => (Action)GetValue(ViewDisplayedActionProperty);
        set => SetValue(ViewDisplayedActionProperty, value);
    }

    private ScrollViewer _scrollViewer;
    private bool _wasVisible;

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.Unloaded += OnUnloaded;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.Unloaded -= OnUnloaded;
        }
        DetachScrollViewer();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer = AssociatedObject?.FindAscendant<ScrollViewer>();
        if (_scrollViewer != null)
        {
            _scrollViewer.ViewChanged -= OnScrollChanged;
            _scrollViewer.ViewChanged += OnScrollChanged;
        }
        UpdateVisibility();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => DetachScrollViewer();

    private void OnScrollChanged(object sender, ScrollViewerViewChangedEventArgs e) => UpdateVisibility();

    private void DetachScrollViewer()
    {
        _scrollViewer?.ViewChanged -= OnScrollChanged;
        _scrollViewer = null;
    }

    private void UpdateVisibility()
    {
        if (AssociatedObject == null) return;
        bool visible = IsVisibleInViewport(AssociatedObject);
        if (visible && !_wasVisible) ViewDisplayedAction?.Invoke();
        _wasVisible = visible;
    }

    private static bool IsVisibleInViewport(FrameworkElement element)
    {
        var sv = element.FindAscendant<ScrollViewer>();
        if (sv == null) return element.IsLoaded;
        var bounds = element.TransformToVisual(sv).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        return bounds.Right >= 0 && bounds.Bottom >= 0 && bounds.Left <= sv.ViewportWidth && bounds.Top <= sv.ViewportHeight;
    }
}
