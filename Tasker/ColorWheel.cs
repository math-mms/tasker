using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;
using WpfPen = System.Windows.Media.Pen;
using WpfBrushes = System.Windows.Media.Brushes;

namespace Tasker;

public sealed class ColorWheel : FrameworkElement
{
    private const int BitmapSize = 256;
    private WriteableBitmap? _wheelBitmap;
    private double _hue;
    private double _saturation;
    private double _brightness = 1;

    public event EventHandler? ColorChanged;

    public WpfColor SelectedColor => FromHsv(_hue, _saturation, _brightness);

    public double Brightness
    {
        get => _brightness;
        set
        {
            _brightness = Math.Clamp(value, 0, 1);
            _wheelBitmap = null;
            InvalidateVisual();
            ColorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ColorWheel()
    {
        Width = 260;
        Height = 260;
        Focusable = true;
        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
    }

    public void SetColor(WpfColor color)
    {
        ToHsv(color, out _hue, out _saturation, out _brightness);
        _wheelBitmap = null;
        InvalidateVisual();
    }

    protected override WpfSize MeasureOverride(WpfSize availableSize)
    {
        double size = Math.Min(260, Math.Min(availableSize.Width, availableSize.Height));
        if (double.IsInfinity(size) || size <= 0)
            size = 260;
        return new WpfSize(size, size);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        // Keep the entire picker surface hit-testable, including the transparent corners.
        dc.DrawRectangle(WpfBrushes.Transparent, null, new Rect(RenderSize));
        double size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0)
            return;

        _wheelBitmap ??= CreateWheelBitmap();
        var origin = new WpfPoint((ActualWidth - size) / 2, (ActualHeight - size) / 2);
        dc.DrawImage(_wheelBitmap, new Rect(origin, new WpfSize(size, size)));

        double radius = size * 0.46;
        double angle = _hue * Math.PI / 180;
        var marker = new WpfPoint(
            origin.X + size / 2 + Math.Cos(angle) * radius * _saturation,
            origin.Y + size / 2 + Math.Sin(angle) * radius * _saturation);
        dc.DrawEllipse(null, new WpfPen(WpfBrushes.Black, 4), marker, 8, 8);
        dc.DrawEllipse(null, new WpfPen(WpfBrushes.White, 2), marker, 8, 8);
    }

    private WriteableBitmap CreateWheelBitmap()
    {
        var pixels = new byte[BitmapSize * BitmapSize * 4];
        double center = (BitmapSize - 1) / 2d;
        double radius = center * 0.92;

        for (int y = 0; y < BitmapSize; y++)
        for (int x = 0; x < BitmapSize; x++)
        {
            double dx = (x - center) / radius;
            double dy = (y - center) / radius;
            double saturation = Math.Sqrt(dx * dx + dy * dy);
            int offset = (y * BitmapSize + x) * 4;
            if (saturation > 1)
                continue;

            double hue = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
            WpfColor color = FromHsv(hue, saturation, _brightness);
            pixels[offset] = color.B;
            pixels[offset + 1] = color.G;
            pixels[offset + 2] = color.R;
            pixels[offset + 3] = 255;
        }

        var bitmap = new WriteableBitmap(BitmapSize, BitmapSize, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, BitmapSize, BitmapSize), pixels, BitmapSize * 4, 0);
        return bitmap;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        CaptureMouse();
        UpdateFromPoint(e.GetPosition(this));
        e.Handled = true;
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
            UpdateFromPoint(e.GetPosition(this));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (IsMouseCaptured)
            ReleaseMouseCapture();
    }

    private void UpdateFromPoint(WpfPoint point)
    {
        double size = Math.Min(ActualWidth, ActualHeight);
        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;
        double dx = point.X - centerX;
        double dy = point.Y - centerY;
        _hue = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
        _saturation = Math.Clamp(Math.Sqrt(dx * dx + dy * dy) / (size * 0.46), 0, 1);
        InvalidateVisual();
        ColorChanged?.Invoke(this, EventArgs.Empty);
    }

    public static WpfColor FromHsv(double hue, double saturation, double value)
    {
        hue = (hue % 360 + 360) % 360;
        saturation = Math.Clamp(saturation, 0, 1);
        value = Math.Clamp(value, 0, 1);
        double chroma = value * saturation;
        double x = chroma * (1 - Math.Abs((hue / 60) % 2 - 1));
        double m = value - chroma;
        (double r, double g, double b) = hue switch
        {
            < 60 => (chroma, x, 0d),
            < 120 => (x, chroma, 0d),
            < 180 => (0d, chroma, x),
            < 240 => (0d, x, chroma),
            < 300 => (x, 0d, chroma),
            _ => (chroma, 0d, x)
        };
        return WpfColor.FromRgb(ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    private static void ToHsv(WpfColor color, out double hue, out double saturation, out double value)
    {
        double r = color.R / 255d, g = color.G / 255d, b = color.B / 255d;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;
        hue = delta == 0 ? 0 : max == r ? 60 * (((g - b) / delta) % 6) : max == g ? 60 * ((b - r) / delta + 2) : 60 * ((r - g) / delta + 4);
        if (hue < 0) hue += 360;
        saturation = max == 0 ? 0 : delta / max;
        value = max;
    }

    private static byte ToByte(double value) => (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);
}
