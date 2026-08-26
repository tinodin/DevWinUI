using System.Collections;

namespace DevWinUI;

public sealed partial class ScreenshotsViewer
{
    public static readonly DependencyProperty CardDataModelProperty =
        DependencyProperty.Register(nameof(CardDataModel), typeof(ICardDataModel), typeof(ScreenshotsViewer), new PropertyMetadata(null));

    public ICardDataModel CardDataModel
    {
        get => (ICardDataModel)GetValue(CardDataModelProperty);
        set => SetValue(CardDataModelProperty, value);
    }

    public static readonly DependencyProperty IsScrollingEnabledProperty =
        DependencyProperty.Register(nameof(IsScrollingEnabled), typeof(bool), typeof(ScreenshotsViewer), new PropertyMetadata(false));

    public bool IsScrollingEnabled
    {
        get => (bool)GetValue(IsScrollingEnabledProperty);
        set => SetValue(IsScrollingEnabledProperty, value);
    }

    public static readonly DependencyProperty PreviewModeProperty =
        DependencyProperty.Register(nameof(PreviewMode), typeof(PreviewType), typeof(ScreenshotsViewer), new PropertyMetadata(PreviewType.None));

    public PreviewType PreviewMode
    {
        get => (PreviewType)GetValue(PreviewModeProperty);
        set => SetValue(PreviewModeProperty, value);
    }

    public static readonly DependencyProperty PreviewViewModelProperty =
        DependencyProperty.Register(nameof(PreviewViewModel), typeof(PreviewModuleViewModel), typeof(ScreenshotsViewer), new PropertyMetadata(null, OnPreviewViewModelChanged));

    public PreviewModuleViewModel PreviewViewModel
    {
        get => (PreviewModuleViewModel)GetValue(PreviewViewModelProperty);
        set => SetValue(PreviewViewModelProperty, value);
    }

    private static void OnPreviewViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (ScreenshotsViewer)d;
        c.OnPreviewViewModelChanged(e.OldValue as PreviewModuleViewModel, e.NewValue as PreviewModuleViewModel);
        if (c.ScreenshotTileList != null && e.NewValue is PreviewModuleViewModel vm)
            c.ScreenshotTileList.ItemsSource = vm.Items;
        c.UpdateEmptyState();
    }

    public static readonly DependencyProperty ScreenshotContainerStyleProperty =
        DependencyProperty.Register(nameof(ScreenshotContainerStyle), typeof(Style), typeof(ScreenshotsViewer), new PropertyMetadata(null));

    public Style ScreenshotContainerStyle
    {
        get => (Style)GetValue(ScreenshotContainerStyleProperty);
        set => SetValue(ScreenshotContainerStyleProperty, value);
    }

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(ScreenshotsViewer), new PropertyMetadata(0));

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public static readonly DependencyProperty AgeRestrictedProperty =
        DependencyProperty.Register(nameof(AgeRestricted), typeof(bool), typeof(ScreenshotsViewer), new PropertyMetadata(false, OnAgeRestrictedChanged));

    public bool AgeRestricted
    {
        get => (bool)GetValue(AgeRestrictedProperty);
        set => SetValue(AgeRestrictedProperty, value);
    }

    private static void OnAgeRestrictedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewer = (ScreenshotsViewer)d;
        viewer.ApplyAgeRestriction((bool)e.NewValue);
    }

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ScreenshotsViewer), new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (ScreenshotsViewer)d;
        var vm = new PreviewModuleViewModel();
        if (e.NewValue is IEnumerable items)
            foreach (var item in items) vm.Items.Add(item);
        c.PreviewViewModel = vm;
    }
}
