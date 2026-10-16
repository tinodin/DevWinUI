using Microsoft.Xaml.Interactivity;

namespace DevWinUI;

public sealed partial class ImageOpenedEventTriggerBehavior : Trigger<Image>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject?.ImageOpened += OnImageOpened;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject?.ImageOpened -= OnImageOpened;
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        Interaction.ExecuteActions(sender, Actions, e);
    }
}
