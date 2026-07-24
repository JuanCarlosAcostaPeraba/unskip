namespace Unskip.App.Services;

internal readonly record struct VirtualScreenBounds
{
    public VirtualScreenBounds(double left, double top, double width, double height)
    {
        if (!double.IsFinite(left))
        {
            throw new ArgumentOutOfRangeException(nameof(left));
        }

        if (!double.IsFinite(top))
        {
            throw new ArgumentOutOfRangeException(nameof(top));
        }

        if (!double.IsFinite(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (!double.IsFinite(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    public double Left { get; }

    public double Top { get; }

    public double Width { get; }

    public double Height { get; }
}
