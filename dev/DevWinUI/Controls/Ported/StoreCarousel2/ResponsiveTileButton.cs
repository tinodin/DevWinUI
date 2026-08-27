namespace DevWinUI;

public partial class ResponsiveTileButton : Button
{
    public static readonly DependencyProperty ContentWidthProperty =
        DependencyProperty.Register(nameof(ContentWidth), typeof(double), typeof(ResponsiveTileButton), new PropertyMetadata(0.0));

    public double ContentWidth
    {
        get => (double)GetValue(ContentWidthProperty);
        set => SetValue(ContentWidthProperty, value);
    }

    public static readonly DependencyProperty IsFocusRedirectorProperty =
        DependencyProperty.Register(nameof(IsFocusRedirector), typeof(bool), typeof(ResponsiveTileButton), new PropertyMetadata(false));

    public bool IsFocusRedirector
    {
        get => (bool)GetValue(IsFocusRedirectorProperty);
        set => SetValue(IsFocusRedirectorProperty, value);
    }

    public static readonly DependencyProperty TypeNameIdProperty =
        DependencyProperty.Register(nameof(TypeNameId), typeof(string), typeof(ResponsiveTileButton), new PropertyMetadata(null));

    public string TypeNameId
    {
        get => (string)GetValue(TypeNameIdProperty);
        set => SetValue(TypeNameIdProperty, value);
    }

    private Grid _rootGrid;
    private ContentPresenter _contentPresenter;
    private Border _focusRect;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _rootGrid = GetTemplateChild("_rootGrid") as Grid;
        _contentPresenter = GetTemplateChild("_contentPresenter") as ContentPresenter;
        _focusRect = GetTemplateChild("_focusRect") as Border;
    }
}
