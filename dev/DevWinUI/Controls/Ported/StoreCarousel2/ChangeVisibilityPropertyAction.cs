using Microsoft.Xaml.Interactivity;

namespace DevWinUI;

public sealed partial class ChangeVisibilityPropertyAction : DependencyObject, IAction
{
    public static readonly DependencyProperty TargetObjectProperty =
        DependencyProperty.Register(nameof(TargetObject), typeof(DependencyObject), typeof(ChangeVisibilityPropertyAction),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(Visibility), typeof(ChangeVisibilityPropertyAction),
            new PropertyMetadata(Visibility.Visible));

    public DependencyObject TargetObject
    {
        get => (DependencyObject)GetValue(TargetObjectProperty);
        set => SetValue(TargetObjectProperty, value);
    }

    public Visibility Value
    {
        get => (Visibility)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public object Execute(object sender, object parameter)
    {
        if (TargetObject is UIElement target)
        {
            target.Visibility = Value;
        }

        return null;
    }
}
