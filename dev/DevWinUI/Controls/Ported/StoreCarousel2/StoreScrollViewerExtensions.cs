namespace DevWinUI;

public static class StoreScrollViewerExtensions
{
    public static readonly DependencyProperty AutoResetOffsetProperty =
        DependencyProperty.RegisterAttached(
            "AutoResetOffset",
            typeof(bool),
            typeof(StoreScrollViewerExtensions),
            new PropertyMetadata(false, OnAutoResetOffsetChanged));

    public static bool GetAutoResetOffset(DependencyObject obj) => (bool)obj.GetValue(AutoResetOffsetProperty);
    public static void SetAutoResetOffset(DependencyObject obj, bool value) => obj.SetValue(AutoResetOffsetProperty, value);

    private static void OnAutoResetOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement fe)
        {
            if ((bool)e.NewValue)
            {
                fe.Loaded += OnTargetLoaded;
                if (fe.IsLoaded)
                    ResetOffset(fe);
            }
            else
            {
                fe.Loaded -= OnTargetLoaded;
            }
        }
        else if (d is ScrollViewer sv && (bool)e.NewValue)
        {
            sv.Loaded += (s, _) => sv.ChangeView(0, null, null, true);
            if (sv.IsLoaded)
                sv.ChangeView(0, null, null, true);
        }
    }

    private static void OnTargetLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DependencyObject d)
            ResetOffset(d);
    }

    private static void ResetOffset(DependencyObject root)
    {
        if (root is ScrollViewer sv)
        {
            sv.ChangeView(0, null, null, true);
            return;
        }
        var inner = FindDescendant<ScrollViewer>(root);
        inner?.ChangeView(0, null, null, true);
    }

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
}
