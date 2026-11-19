using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace DevWinUI;

public partial class ConstrainedBox : ContentPresenter
{
    private const double CalculationTolerance = 1.5;

    private Size _originalSize;
    private Size _lastMeasuredSize;

    private bool IsPositiveRealNumber(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;

    protected override Size MeasureOverride(Size availableSize)
    {
        _originalSize = availableSize;

        CalculateConstrainedSize(ref availableSize);

        _lastMeasuredSize = availableSize;

        base.MeasureOverride(_lastMeasuredSize);
        return _lastMeasuredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Math.Abs(finalSize.Width - _lastMeasuredSize.Width) > CalculationTolerance ||
            Math.Abs(finalSize.Height - _lastMeasuredSize.Height) > CalculationTolerance)
        {
            if (Math.Abs(finalSize.Width - _originalSize.Width) <= CalculationTolerance &&
                Math.Abs(finalSize.Height - _originalSize.Height) <= CalculationTolerance)
            {
                finalSize = _lastMeasuredSize;
            }
            else
            {
                CalculateConstrainedSize(ref finalSize);

                _lastMeasuredSize = finalSize;
            }
        }

        return base.ArrangeOverride(finalSize);
    }

    private void CalculateConstrainedSize(ref Size availableSize)
    {
        var hasWidth = IsPositiveRealNumber(availableSize.Width);
        var hasHeight = IsPositiveRealNumber(availableSize.Height);

        if (!hasWidth && !hasHeight)
        {
            availableSize = base.MeasureOverride(availableSize);

            hasWidth = IsPositiveRealNumber(availableSize.Width);
            hasHeight = IsPositiveRealNumber(availableSize.Height);

            if (!hasWidth && !hasHeight)
            {
                return;
            }
        }

        availableSize.Width *= ScaleX;
        availableSize.Height *= ScaleY;

        if (hasWidth &&
            ReadLocalValue(MultipleXProperty) != DependencyProperty.UnsetValue &&
            MultipleX > 0)
        {
            availableSize.Width -= availableSize.Width % MultipleX;
        }

        if (hasHeight &&
            ReadLocalValue(MultipleYProperty) != DependencyProperty.UnsetValue &&
            MultipleY > 0)
        {
            availableSize.Height -= availableSize.Height % MultipleY;
        }

        if (ReadLocalValue(AspectRatioProperty) == DependencyProperty.UnsetValue)
        {
            return;
        }

        var currentAspect = availableSize.Width / availableSize.Height;

        if (!hasWidth)
        {
            availableSize.Width = availableSize.Height * AspectRatio;
        }
        else if (!hasHeight)
        {
            availableSize.Height = availableSize.Width / AspectRatio;
        }
        else if (currentAspect > AspectRatio)
        {
            availableSize.Width = availableSize.Height * AspectRatio;
        }
        else
        {
            availableSize.Height = availableSize.Width / AspectRatio;
        }
    }

    public double ScaleX
    {
        get { return (double)GetValue(ScaleXProperty); }
        set { SetValue(ScaleXProperty, value); }
    }

    public static readonly DependencyProperty ScaleXProperty =
        DependencyProperty.Register(nameof(ScaleX), typeof(double), typeof(ConstrainedBox), new PropertyMetadata(1.0, ConstraintPropertyChanged));

    public double ScaleY
    {
        get { return (double)GetValue(ScaleYProperty); }
        set { SetValue(ScaleYProperty, value); }
    }

    public static readonly DependencyProperty ScaleYProperty =
        DependencyProperty.Register(nameof(ScaleY), typeof(double), typeof(ConstrainedBox), new PropertyMetadata(1.0, ConstraintPropertyChanged));

    public int MultipleX
    {
        get { return (int)GetValue(MultipleXProperty); }
        set { SetValue(MultipleXProperty, value); }
    }

    public static readonly DependencyProperty MultipleXProperty =
        DependencyProperty.Register(nameof(MultipleX), typeof(int), typeof(ConstrainedBox), new PropertyMetadata(null, ConstraintPropertyChanged));

    public int MultipleY
    {
        get { return (int)GetValue(MultipleYProperty); }
        set { SetValue(MultipleYProperty, value); }
    }

    public static readonly DependencyProperty MultipleYProperty =
        DependencyProperty.Register(nameof(MultipleY), typeof(int), typeof(ConstrainedBox), new PropertyMetadata(null, ConstraintPropertyChanged));

    public AspectRatio AspectRatio
    {
        get { return (AspectRatio)GetValue(AspectRatioProperty); }
        set { SetValue(AspectRatioProperty, value); }
    }

    public static readonly DependencyProperty AspectRatioProperty =
        DependencyProperty.Register(nameof(AspectRatio), typeof(AspectRatio), typeof(ConstrainedBox), new PropertyMetadata(null, ConstraintPropertyChanged));

    private bool _propertyUpdating;

    private static void ConstraintPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ConstrainedBox panel && !panel._propertyUpdating)
        {
            panel._propertyUpdating = true;

            panel.CoerceValues();

            panel.InvalidateMeasure();

            panel._propertyUpdating = false;
        }
    }

    private void CoerceValues()
    {
        if (!double.IsNaN(ScaleX))
        {
            if (ScaleX < 0)
            {
                ScaleX = 0;
            }
            else if (ScaleX > 1.0)
            {
                ScaleX = 1.0;
            }
        }
        else
        {
            ScaleX = 1.0;
        }

        if (!double.IsNaN(ScaleY))
        {
            if (ScaleY < 0)
            {
                ScaleY = 0;
            }
            else if (ScaleY > 1.0)
            {
                ScaleY = 1.0;
            }
        }
        else
        {
            ScaleY = 1.0;
        }

        if (ReadLocalValue(MultipleXProperty) is int value && value <= 0)
        {
            ClearValue(MultipleXProperty);
        }

        if (ReadLocalValue(MultipleYProperty) is int value2 && value2 <= 0)
        {
            ClearValue(MultipleYProperty);
        }

        if (ReadLocalValue(AspectRatioProperty) is AspectRatio ratio && ratio <= 0)
        {
            ClearValue(AspectRatioProperty);
        }
    }
}
