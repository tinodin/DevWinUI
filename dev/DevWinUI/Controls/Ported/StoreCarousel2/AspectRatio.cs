using System.Globalization;

namespace DevWinUI;

[Windows.Foundation.Metadata.CreateFromString(MethodName = "DevWinUI.AspectRatio.ConvertToAspectRatio")]
public readonly struct AspectRatio
{
    public double Width { get; }

    public double Height { get; }

    public double Value => Width / Height;

    public AspectRatio(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public AspectRatio(double ratio)
    {
        Width = ratio;
        Height = 1;
    }

    public static implicit operator double(AspectRatio aspect) => aspect.Value;

    public static implicit operator AspectRatio(double ratio) => new(ratio);

    public static implicit operator AspectRatio(int width) => new(width, 1.0);

    public static AspectRatio ConvertToAspectRatio(string rawString)
    {
        string[] ratio = rawString.Split(":");

        if (ratio.Length == 2)
        {
            double width = double.Parse(ratio[0], NumberStyles.Float, CultureInfo.InvariantCulture);
            double height = double.Parse(ratio[1], NumberStyles.Float, CultureInfo.InvariantCulture);

            return new AspectRatio(width, height);
        }
        else if (ratio.Length == 1)
        {
            return new AspectRatio(double.Parse(ratio[0], NumberStyles.Float, CultureInfo.InvariantCulture));
        }

        return new AspectRatio(1);
    }

    public override string ToString()
    {
        return Width + ":" + Height;
    }
}
