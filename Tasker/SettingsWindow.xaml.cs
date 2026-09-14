using System.Windows;
using System.Windows.Controls;

namespace Tasker;

public partial class SettingsWindow : Window
{
    private AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        InitializeControls();
    }

    private void InitializeControls()
    {
        // Widget Tab - Sliders
        OpacitySlider.Value = _settings.WidgetOpacity;
        MaxWidthSlider.Value = _settings.WidgetMaxWidth;
        WidgetTitleSizeSlider.Value = _settings.WidgetTitleFontSize;
        WidgetSubtitleSizeSlider.Value = _settings.WidgetSubtitleFontSize;

        // Widget Tab - Colors
        WidgetBgColorInput.Text = _settings.WidgetBackgroundColor;
        WidgetTitleColorInput.Text = _settings.WidgetTitleColor;
        WidgetTextColorInput.Text = _settings.WidgetSubtitleColor;

        // App Colors Tab
        AppBgColorInput.Text = _settings.AppBackgroundColor;
        AppPanelColorInput.Text = _settings.AppPanelColor;
        AppTextColorInput.Text = _settings.AppTextColor;
        AppSecTextColorInput.Text = _settings.AppSecondaryTextColor;
        AppPrimaryBtnColorInput.Text = _settings.AppPrimaryButtonColor;
        AppSecBtnColorInput.Text = _settings.AppSecondaryButtonColor;
        AppBorderColorInput.Text = _settings.AppBorderColor;

        // App Fonts Tab
        AppTitleSizeSlider.Value = _settings.AppTitleFontSize;
        AppHeadingSizeSlider.Value = _settings.AppHeadingFontSize;
        AppTextSizeSlider.Value = _settings.AppTextFontSize;
        AppLabelSizeSlider.Value = _settings.AppLabelFontSize;

        // Attach slider value changed events
        OpacitySlider.ValueChanged += OpacitySlider_ValueChanged;
        MaxWidthSlider.ValueChanged += MaxWidthSlider_ValueChanged;
        WidgetTitleSizeSlider.ValueChanged += WidgetTitleSizeSlider_ValueChanged;
        WidgetSubtitleSizeSlider.ValueChanged += WidgetSubtitleSizeSlider_ValueChanged;
        AppTitleSizeSlider.ValueChanged += AppTitleSizeSlider_ValueChanged;
        AppHeadingSizeSlider.ValueChanged += AppHeadingSizeSlider_ValueChanged;
        AppTextSizeSlider.ValueChanged += AppTextSizeSlider_ValueChanged;
        AppLabelSizeSlider.ValueChanged += AppLabelSizeSlider_ValueChanged;
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _settings.WidgetOpacity = e.NewValue;
        OpacityValue.Text = $"{(int)(e.NewValue * 100)}%";
    }

    private void MaxWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _settings.WidgetMaxWidth = (int)e.NewValue;
        MaxWidthValue.Text = e.NewValue.ToString("F0");
    }

    private void WidgetTitleSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _settings.WidgetTitleFontSize = (int)e.NewValue;
        WidgetTitleSizeValue.Text = e.NewValue.ToString("F0");
    }

    private void WidgetSubtitleSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _settings.WidgetSubtitleFontSize = (int)e.NewValue;
        WidgetSubtitleSizeValue.Text = e.NewValue.ToString("F0");
    }

    private void AppTitleSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _settings.AppTitleFontSize = (int)e.NewValue;
        AppTitleSizeValue.Text = e.NewValue.ToString("F0");
    }

    private void AppHeadingSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _settings.AppHeadingFontSize = (int)e.NewValue;
        AppHeadingSizeValue.Text = e.NewValue.ToString("F0");
    }

    private void AppTextSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _settings.AppTextFontSize = (int)e.NewValue;
        AppTextSizeValue.Text = e.NewValue.ToString("F0");
    }

    private void AppLabelSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _settings.AppLabelFontSize = (int)e.NewValue;
        AppLabelSizeValue.Text = e.NewValue.ToString("F0");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Widget Colors
        _settings.WidgetBackgroundColor = WidgetBgColorInput.Text;
        _settings.WidgetTitleColor = WidgetTitleColorInput.Text;
        _settings.WidgetSubtitleColor = WidgetTextColorInput.Text;
        _settings.WidgetDescriptionColor = WidgetTextColorInput.Text;

        // App Colors
        _settings.AppBackgroundColor = AppBgColorInput.Text;
        _settings.AppPanelColor = AppPanelColorInput.Text;
        _settings.AppTextColor = AppTextColorInput.Text;
        _settings.AppSecondaryTextColor = AppSecTextColorInput.Text;
        _settings.AppPrimaryButtonColor = AppPrimaryBtnColorInput.Text;
        _settings.AppSecondaryButtonColor = AppSecBtnColorInput.Text;
        _settings.AppBorderColor = AppBorderColorInput.Text;

        // Save settings
        _settings.Save();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
