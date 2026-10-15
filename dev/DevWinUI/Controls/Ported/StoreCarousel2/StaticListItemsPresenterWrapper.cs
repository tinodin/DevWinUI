using Microsoft.UI.Xaml.Input;

namespace DevWinUI;

public partial class StaticListItemsPresenterWrapper : ContentControl
{
    public StaticListItemsPresenterWrapper()
    {
        DefaultStyleKey = typeof(ContentControl);
        IsTabStop = false;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;

        AddHandler(PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChangedInternal), handledEventsToo: true);
    }

    private void OnPointerWheelChangedInternal(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsHorizontalMouseWheel)
        {
            e.Handled = true;
        }
    }

    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsHorizontalMouseWheel)
        {
            e.Handled = true;
        }
        base.OnPointerWheelChanged(e);
    }
}
