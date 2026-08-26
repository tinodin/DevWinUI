using System.Collections;

namespace DevWinUI;

public partial class StoreCarousel2
{
    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(StoreCarousel2), new PropertyMetadata(null, OnItemsSourceChanged));

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (StoreCarousel2)d;
        if (c._card != null)
        {
            var vm = new PreviewModuleViewModel();
            if (e.NewValue is IEnumerable items)
                foreach (var item in items) vm.Items.Add(item);
            c._card.PreviewViewModel = vm;
            c._card.AgeRestricted = c.AgeRestricted;
        }
        if (c._viewer != null)
        {
            var pvm = new PreviewItemsViewModel();
            if (e.NewValue is IEnumerable vitems)
                foreach (var item in vitems) pvm.Items.Add(item);
            c._viewer.ViewModel = pvm;
            c._viewer.AgeRestricted = c.AgeRestricted;
        }
    }

    public bool AgeRestricted
    {
        get => (bool)GetValue(AgeRestrictedProperty);
        set => SetValue(AgeRestrictedProperty, value);
    }

    public static readonly DependencyProperty AgeRestrictedProperty =
        DependencyProperty.Register(nameof(AgeRestricted), typeof(bool), typeof(StoreCarousel2), new PropertyMetadata(false, OnAgeRestrictedChanged));

    private static void OnAgeRestrictedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var carousel = (StoreCarousel2)d;
        if (carousel._card != null)
            carousel._card.AgeRestricted = (bool)e.NewValue;
        if (carousel._viewer != null)
            carousel._viewer.AgeRestricted = (bool)e.NewValue;
    }

    public bool IsViewerEnabled
    {
        get => (bool)GetValue(IsViewerEnabledProperty);
        set => SetValue(IsViewerEnabledProperty, value);
    }

    public static readonly DependencyProperty IsViewerEnabledProperty =
        DependencyProperty.Register(nameof(IsViewerEnabled), typeof(bool), typeof(StoreCarousel2), new PropertyMetadata(true));
}
