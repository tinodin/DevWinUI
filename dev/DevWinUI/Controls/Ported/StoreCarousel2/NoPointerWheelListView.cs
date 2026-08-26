using Microsoft.UI.Xaml.Input;

namespace DevWinUI;

public partial class NoPointerWheelListView : ListView
{
    public static readonly DependencyProperty DisposeOnUnloadedProperty =
        DependencyProperty.Register(nameof(DisposeOnUnloaded), typeof(bool), typeof(NoPointerWheelListView), new PropertyMetadata(false));

    public bool DisposeOnUnloaded
    {
        get => (bool)GetValue(DisposeOnUnloadedProperty);
        set => SetValue(DisposeOnUnloadedProperty, value);
    }

    public static readonly DependencyProperty HorizontalScrollModeProperty =
        DependencyProperty.Register(nameof(HorizontalScrollMode), typeof(ScrollMode), typeof(NoPointerWheelListView), new PropertyMetadata(default(ScrollMode)));

    public ScrollMode HorizontalScrollMode
    {
        get => (ScrollMode)GetValue(HorizontalScrollModeProperty);
        set => SetValue(HorizontalScrollModeProperty, value);
    }

    public static readonly DependencyProperty RemoveSelectedItemOnUpDownNavigationProperty =
        DependencyProperty.Register(nameof(RemoveSelectedItemOnUpDownNavigation), typeof(bool), typeof(NoPointerWheelListView), new PropertyMetadata(false));

    public bool RemoveSelectedItemOnUpDownNavigation
    {
        get => (bool)GetValue(RemoveSelectedItemOnUpDownNavigationProperty);
        set => SetValue(RemoveSelectedItemOnUpDownNavigationProperty, value);
    }

    public static readonly DependencyProperty ScrollViewerMarginProperty =
        DependencyProperty.Register(nameof(ScrollViewerMargin), typeof(Thickness), typeof(NoPointerWheelListView), new PropertyMetadata(default(Thickness)));

    public Thickness ScrollViewerMargin
    {
        get => (Thickness)GetValue(ScrollViewerMarginProperty);
        set => SetValue(ScrollViewerMarginProperty, value);
    }

    public static readonly DependencyProperty ScrollViewerPaddingProperty =
        DependencyProperty.Register(nameof(ScrollViewerPadding), typeof(Thickness), typeof(NoPointerWheelListView), new PropertyMetadata(default(Thickness)));

    public Thickness ScrollViewerPadding
    {
        get => (Thickness)GetValue(ScrollViewerPaddingProperty);
        set => SetValue(ScrollViewerPaddingProperty, value);
    }

    private ScrollViewer _scrollViewer;

    public NoPointerWheelListView()
    {
        Unloaded += OnUnloaded;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
        if (_scrollViewer != null)
        {
            _scrollViewer.HorizontalScrollMode = HorizontalScrollMode;
            if (ScrollViewerMargin != default)
                _scrollViewer.Margin = ScrollViewerMargin;
            if (ScrollViewerPadding != default)
                _scrollViewer.Padding = ScrollViewerPadding;
        }
    }

    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        e.Handled = true;
        base.OnPointerWheelChanged(e);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DisposeOnUnloaded)
            ItemsSource = null;
    }
}
