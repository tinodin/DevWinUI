using System.Collections;

namespace DevWinUI;

public partial class StoreCarousel2
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(StoreCarousel2), new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

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
            c._card.IsAgeBlocked = c.IsAgeBlocked;
        }

        if (c._card?.TileList != null)
        {
            c.ReplaceViewerViewModelItems();
            c.EnsureHelper();
        }
    }

    public static readonly DependencyProperty AgeRestrictedProperty =
        DependencyProperty.Register(nameof(AgeRestricted), typeof(bool), typeof(StoreCarousel2), new PropertyMetadata(false, OnAgeRestrictedChanged));

    public bool AgeRestricted
    {
        get => (bool)GetValue(AgeRestrictedProperty);
        set => SetValue(AgeRestrictedProperty, value);
    }

    private static void OnAgeRestrictedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var carousel = (StoreCarousel2)d;
        carousel._card?.AgeRestricted = (bool)e.NewValue;
        carousel._viewer?.AgeRestricted = (bool)e.NewValue;
    }

    public static readonly DependencyProperty IsAgeBlockedProperty =
        DependencyProperty.Register(nameof(IsAgeBlocked), typeof(bool), typeof(StoreCarousel2), new PropertyMetadata(false, OnIsAgeBlockedChanged));

    public bool IsAgeBlocked
    {
        get => (bool)GetValue(IsAgeBlockedProperty);
        set => SetValue(IsAgeBlockedProperty, value);
    }

    private static void OnIsAgeBlockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var carousel = (StoreCarousel2)d;
        carousel._card?.IsAgeBlocked = (bool)e.NewValue;
        carousel._viewer?.IsAgeBlocked = (bool)e.NewValue;
    }

    public static readonly DependencyProperty IsViewerEnabledProperty =
        DependencyProperty.Register(nameof(IsViewerEnabled), typeof(bool), typeof(StoreCarousel2), new PropertyMetadata(true, OnIsViewerEnabledChanged));

    public bool IsViewerEnabled
    {
        get => (bool)GetValue(IsViewerEnabledProperty);
        set => SetValue(IsViewerEnabledProperty, value);
    }

    private static void OnIsViewerEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (StoreCarousel2)d;
        if (c._card == null)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            c.EnsureHelper();
        }
        else
        {
            c._card.Helper = null;
        }
    }
}
