using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using ColorConverter = System.Windows.Media.ColorConverter;
using WpfColor = System.Windows.Media.Color;
using Button = System.Windows.Controls.Button;

namespace Tasker;

public partial class ColorPickerWindow : Window
{
    private bool _updating;

    public WpfColor SelectedColor => Wheel.SelectedColor;

    public ColorPickerWindow(WpfColor initialColor)
    {
        InitializeComponent();
        Wheel.SetColor(initialColor);
        BrightnessSlider.Value = Wheel.Brightness;
        HexInput.TextChanged += HexInput_TextChanged;
        Wheel.ColorChanged += Wheel_ColorChanged;
        BrightnessSlider.ValueChanged += BrightnessSlider_ValueChanged;
        RefreshSelectedColor();
    }

    private void Wheel_ColorChanged(object? sender, EventArgs e)
    {
        if (_updating)
            return;
        _updating = true;
        BrightnessSlider.Value = Wheel.Brightness;
        HexInput.Text = Wheel.SelectedColor.ToString();
        ColorPreview.Background = new SolidColorBrush(Wheel.SelectedColor);
        _updating = false;
    }

    private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating)
            return;
        Wheel.Brightness = e.NewValue;
        RefreshSelectedColor();
    }

    private void HexInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || !TryParseColor(HexInput.Text, out var color))
            return;
        _updating = true;
        Wheel.SetColor(color);
        BrightnessSlider.Value = Wheel.Brightness;
        ColorPreview.Background = new SolidColorBrush(color);
        _updating = false;
    }

    private void PresetColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string value } && TryParseColor(value, out var color))
        {
            _updating = true;
            Wheel.SetColor(color);
            BrightnessSlider.Value = Wheel.Brightness;
            _updating = false;
            RefreshSelectedColor();
        }
    }

    private void UseColor_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseColor(HexInput.Text, out _))
        {
            MessageBox.Show(this, "Enter a valid hex color first.", "Invalid color",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void RefreshSelectedColor()
    {
        _updating = true;
        HexInput.Text = Wheel.SelectedColor.ToString();
        ColorPreview.Background = new SolidColorBrush(Wheel.SelectedColor);
        _updating = false;
    }

    private static bool TryParseColor(string value, out WpfColor color)
    {
        try
        {
            if (ColorConverter.ConvertFromString(value) is WpfColor parsed)
            {
                color = parsed;
                return true;
            }
        }
        catch
        {
        }
        color = Colors.Transparent;
        return false;
    }
}
