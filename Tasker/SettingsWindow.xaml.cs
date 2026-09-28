using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;

namespace Tasker;

public partial class SettingsWindow : Window
{
    private AppSettings _settings;
    private readonly AppSettings _savedSettings;
    private bool _loadingControls;
    public event Action? SettingsChanged;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        _savedSettings = settings.Clone();
        InitializeControls();
        RefreshPreview();
    }

    private void InitializeControls()
    {
        LoadControlsFromSettings();

        // Attach slider value changed events
        OpacitySlider.ValueChanged += OpacitySlider_ValueChanged;
        MaxWidthSlider.ValueChanged += MaxWidthSlider_ValueChanged;
        WidgetTitleSizeSlider.ValueChanged += WidgetTitleSizeSlider_ValueChanged;
        WidgetSubtitleSizeSlider.ValueChanged += WidgetSubtitleSizeSlider_ValueChanged;
        AppTitleSizeSlider.ValueChanged += AppTitleSizeSlider_ValueChanged;
        AppHeadingSizeSlider.ValueChanged += AppHeadingSizeSlider_ValueChanged;
        AppTextSizeSlider.ValueChanged += AppTextSizeSlider_ValueChanged;
        AppLabelSizeSlider.ValueChanged += AppLabelSizeSlider_ValueChanged;

        foreach (var input in new[]
        {
            WidgetBgColorInput, WidgetTitleColorInput, WidgetTextColorInput,
            AppBgColorInput, AppPanelColorInput, AppTextColorInput, AppSecTextColorInput,
            AppPrimaryBtnColorInput, AppSecBtnColorInput, AppBorderColorInput,
            TaskCardBgInput, TaskCardTextInput, TaskCardSubtitleInput, CompletedTaskTextInput,
            TaskCardBorderInput, CheckboxAccentInput
        })
            input.TextChanged += ColorInput_TextChanged;

        WidgetFontFamilyInput.TextChanged += FontFamilyInput_TextChanged;
        AppFontFamilyInput.TextChanged += FontFamilyInput_TextChanged;
    }

    private void LoadControlsFromSettings()
    {
        _loadingControls = true;
        try
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
        WidgetFontFamilyInput.Text = _settings.WidgetFontFamily;

        // App Colors Tab
        AppBgColorInput.Text = _settings.AppBackgroundColor;
        AppPanelColorInput.Text = _settings.AppPanelColor;
        AppTextColorInput.Text = _settings.AppTextColor;
        AppSecTextColorInput.Text = _settings.AppSecondaryTextColor;
        AppPrimaryBtnColorInput.Text = _settings.AppPrimaryButtonColor;
        AppSecBtnColorInput.Text = _settings.AppSecondaryButtonColor;
        AppBorderColorInput.Text = _settings.AppBorderColor;
        TaskCardBgInput.Text = string.IsNullOrWhiteSpace(_settings.AppTaskCardBackgroundColor) ? _settings.AppPanelColor : _settings.AppTaskCardBackgroundColor;
        TaskCardTextInput.Text = string.IsNullOrWhiteSpace(_settings.AppTaskCardTextColor) ? _settings.AppTextColor : _settings.AppTaskCardTextColor;
        TaskCardSubtitleInput.Text = string.IsNullOrWhiteSpace(_settings.AppTaskCardSubtitleColor) ? _settings.AppSecondaryTextColor : _settings.AppTaskCardSubtitleColor;
        CompletedTaskTextInput.Text = _settings.AppCompletedTaskTextColor;
        TaskCardBorderInput.Text = string.IsNullOrWhiteSpace(_settings.AppTaskCardBorderColor) ? _settings.AppBorderColor : _settings.AppTaskCardBorderColor;
        CheckboxAccentInput.Text = string.IsNullOrWhiteSpace(_settings.AppCheckboxAccentColor) ? _settings.AppPrimaryButtonColor : _settings.AppCheckboxAccentColor;
        AppFontFamilyInput.Text = _settings.AppFontFamily;

        // App Fonts Tab
        AppTitleSizeSlider.Value = _settings.AppTitleFontSize;
        AppHeadingSizeSlider.Value = _settings.AppHeadingFontSize;
        AppTextSizeSlider.Value = _settings.AppTextFontSize;
        AppLabelSizeSlider.Value = _settings.AppLabelFontSize;

        OpacityValue.Text = $"{(int)(_settings.WidgetOpacity * 100)}%";
        MaxWidthValue.Text = _settings.WidgetMaxWidth.ToString();
        WidgetTitleSizeValue.Text = _settings.WidgetTitleFontSize.ToString();
        WidgetSubtitleSizeValue.Text = _settings.WidgetSubtitleFontSize.ToString();
        AppTitleSizeValue.Text = _settings.AppTitleFontSize.ToString();
        AppHeadingSizeValue.Text = _settings.AppHeadingFontSize.ToString();
        AppTextSizeValue.Text = _settings.AppTextFontSize.ToString();
        AppLabelSizeValue.Text = _settings.AppLabelFontSize.ToString();
        }
        finally
        {
            _loadingControls = false;
        }
    }

    private void ColorInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingControls)
            return;
        if (sender is not TextBox input || !IsValidColor(input.Text))
            return;

        switch (input.Name)
        {
            case "WidgetBgColorInput": _settings.WidgetBackgroundColor = input.Text; break;
            case "WidgetTitleColorInput": _settings.WidgetTitleColor = input.Text; break;
            case "WidgetTextColorInput":
                _settings.WidgetSubtitleColor = input.Text;
                _settings.WidgetDescriptionColor = input.Text;
                break;
            case "AppBgColorInput": _settings.AppBackgroundColor = input.Text; break;
            case "AppPanelColorInput": _settings.AppPanelColor = input.Text; break;
            case "AppTextColorInput": _settings.AppTextColor = input.Text; break;
            case "AppSecTextColorInput": _settings.AppSecondaryTextColor = input.Text; break;
            case "AppPrimaryBtnColorInput": _settings.AppPrimaryButtonColor = input.Text; break;
            case "AppSecBtnColorInput": _settings.AppSecondaryButtonColor = input.Text; break;
            case "AppBorderColorInput": _settings.AppBorderColor = input.Text; break;
            case "TaskCardBgInput": _settings.AppTaskCardBackgroundColor = input.Text; break;
            case "TaskCardTextInput": _settings.AppTaskCardTextColor = input.Text; break;
            case "TaskCardSubtitleInput": _settings.AppTaskCardSubtitleColor = input.Text; break;
            case "CompletedTaskTextInput": _settings.AppCompletedTaskTextColor = input.Text; break;
            case "TaskCardBorderInput": _settings.AppTaskCardBorderColor = input.Text; break;
            case "CheckboxAccentInput": _settings.AppCheckboxAccentColor = input.Text; break;
        }

        NotifySettingsChanged();
    }

    private void FontFamilyInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingControls)
            return;
        if (sender is not TextBox input || string.IsNullOrWhiteSpace(input.Text))
            return;
        if (input.Name == nameof(AppFontFamilyInput))
            _settings.AppFontFamily = input.Text.Trim();
        else
            _settings.WidgetFontFamily = input.Text.Trim();
        NotifySettingsChanged();
    }

    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string inputName } || FindColorInput(inputName) is not TextBox input)
            return;

        var startingColor = System.Windows.Media.Colors.White;
        try
        {
            if (System.Windows.Media.ColorConverter.ConvertFromString(input.Text) is System.Windows.Media.Color color)
                startingColor = color;
        }
        catch
        {
        }

        var picker = new ColorPickerWindow(startingColor) { Owner = this };
        if (picker.ShowDialog() == true)
            input.Text = picker.SelectedColor.ToString();
    }

    private TextBox? FindColorInput(string name) => name switch
    {
        nameof(WidgetBgColorInput) => WidgetBgColorInput,
        nameof(WidgetTitleColorInput) => WidgetTitleColorInput,
        nameof(WidgetTextColorInput) => WidgetTextColorInput,
        nameof(AppBgColorInput) => AppBgColorInput,
        nameof(AppPanelColorInput) => AppPanelColorInput,
        nameof(AppTextColorInput) => AppTextColorInput,
        nameof(AppSecTextColorInput) => AppSecTextColorInput,
        nameof(AppPrimaryBtnColorInput) => AppPrimaryBtnColorInput,
        nameof(AppSecBtnColorInput) => AppSecBtnColorInput,
        nameof(AppBorderColorInput) => AppBorderColorInput,
        nameof(TaskCardBgInput) => TaskCardBgInput,
        nameof(TaskCardTextInput) => TaskCardTextInput,
        nameof(TaskCardSubtitleInput) => TaskCardSubtitleInput,
        nameof(CompletedTaskTextInput) => CompletedTaskTextInput,
        nameof(TaskCardBorderInput) => TaskCardBorderInput,
        nameof(CheckboxAccentInput) => CheckboxAccentInput,
        _ => null
    };

    private static bool IsValidColor(string value)
    {
        try
        {
            return System.Windows.Media.ColorConverter.ConvertFromString(value) is System.Windows.Media.Color;
        }
        catch
        {
            return false;
        }
    }

    private void NotifySettingsChanged()
    {
        RefreshPreview();
        SettingsChanged?.Invoke();
    }

    private void RefreshPreview()
    {
        System.Windows.Media.SolidColorBrush Brush(string value) => new(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(value));
        System.Windows.Media.FontFamily Font(string value)
        {
            try { return new System.Windows.Media.FontFamily(value); }
            catch { return new System.Windows.Media.FontFamily("Segoe UI"); }
        }

        string card = string.IsNullOrWhiteSpace(_settings.AppTaskCardBackgroundColor) ? _settings.AppPanelColor : _settings.AppTaskCardBackgroundColor;
        string title = string.IsNullOrWhiteSpace(_settings.AppTaskCardTextColor) ? _settings.AppTextColor : _settings.AppTaskCardTextColor;
        string subtitle = string.IsNullOrWhiteSpace(_settings.AppTaskCardSubtitleColor) ? _settings.AppSecondaryTextColor : _settings.AppTaskCardSubtitleColor;
        string border = string.IsNullOrWhiteSpace(_settings.AppTaskCardBorderColor) ? _settings.AppBorderColor : _settings.AppTaskCardBorderColor;
        string accent = string.IsNullOrWhiteSpace(_settings.AppCheckboxAccentColor) ? _settings.AppPrimaryButtonColor : _settings.AppCheckboxAccentColor;

        PreviewTaskCard.Background = Brush(card);
        PreviewTaskCard.BorderBrush = Brush(border);
        PreviewTaskTitle.Foreground = Brush(title);
        PreviewTaskTitle.FontSize = _settings.AppTextFontSize;
        PreviewTaskSubtitle.Foreground = Brush(subtitle);
        PreviewTaskSubtitle.FontSize = _settings.AppLabelFontSize;
        PreviewCompletedTask.Foreground = Brush(_settings.AppCompletedTaskTextColor);
        PreviewCompletedTask.FontSize = Math.Max(9, _settings.AppLabelFontSize - 1);
        PreviewCheckbox.Background = Brush(accent);
        PreviewCheckbox.BorderBrush = Brush(accent);
        PreviewPrimaryButton.Background = Brush(_settings.AppPrimaryButtonColor);
        PreviewSecondaryButton.Background = Brush(_settings.AppSecondaryButtonColor);
        PreviewTaskTitle.FontFamily = Font(_settings.AppFontFamily);
        PreviewTaskSubtitle.FontFamily = Font(_settings.AppFontFamily);
        PreviewCompletedTask.FontFamily = Font(_settings.AppFontFamily);
        PreviewPrimaryButton.FontFamily = Font(_settings.AppFontFamily);
        PreviewSecondaryButton.FontFamily = Font(_settings.AppFontFamily);

        PreviewWidgetCard.Background = Brush(_settings.WidgetBackgroundColor);
        PreviewWidgetCard.BorderBrush = Brush(_settings.WidgetBorderColor);
        PreviewWidgetTitle.Foreground = Brush(_settings.WidgetTitleColor);
        PreviewWidgetTitle.FontFamily = Font(_settings.WidgetFontFamily);
        PreviewWidgetTitle.FontSize = _settings.WidgetTitleFontSize;
        PreviewWidgetSubtitle.Foreground = Brush(_settings.WidgetSubtitleColor);
        PreviewWidgetSubtitle.FontFamily = Font(_settings.WidgetFontFamily);
        PreviewWidgetSubtitle.FontSize = _settings.WidgetSubtitleFontSize;
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loadingControls) return;
        _settings.WidgetOpacity = e.NewValue;
        OpacityValue.Text = $"{(int)(e.NewValue * 100)}%";

        NotifySettingsChanged();
    }

    private void MaxWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loadingControls) return;
        _settings.WidgetMaxWidth = (int)e.NewValue;
        MaxWidthValue.Text = e.NewValue.ToString("F0");

        NotifySettingsChanged();
    }

    private void WidgetTitleSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loadingControls) return;
        _settings.WidgetTitleFontSize = (int)e.NewValue;
        WidgetTitleSizeValue.Text = e.NewValue.ToString("F0");

        NotifySettingsChanged();
    }

    private void WidgetSubtitleSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loadingControls) return;
        _settings.WidgetSubtitleFontSize = (int)e.NewValue;
        WidgetSubtitleSizeValue.Text = e.NewValue.ToString("F0");

        NotifySettingsChanged();
    }

    private void AppTitleSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loadingControls) return;
        _settings.AppTitleFontSize = (int)e.NewValue;
        AppTitleSizeValue.Text = e.NewValue.ToString("F0");

        NotifySettingsChanged();
    }

    private void AppHeadingSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loadingControls) return;
        _settings.AppHeadingFontSize = (int)e.NewValue;
        AppHeadingSizeValue.Text = e.NewValue.ToString("F0");

        NotifySettingsChanged();
    }

    private void AppTextSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loadingControls) return;
        _settings.AppTextFontSize = (int)e.NewValue;
        AppTextSizeValue.Text = e.NewValue.ToString("F0");

        NotifySettingsChanged();
    }

    private void AppLabelSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loadingControls) return;
        _settings.AppLabelFontSize = (int)e.NewValue;
        AppLabelSizeValue.Text = e.NewValue.ToString("F0");

        NotifySettingsChanged();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var colorInputs = new[]
        {
            WidgetBgColorInput, WidgetTitleColorInput, WidgetTextColorInput,
            AppBgColorInput, AppPanelColorInput, AppTextColorInput, AppSecTextColorInput,
            AppPrimaryBtnColorInput, AppSecBtnColorInput, AppBorderColorInput,
            TaskCardBgInput, TaskCardTextInput, TaskCardSubtitleInput, CompletedTaskTextInput,
            TaskCardBorderInput, CheckboxAccentInput
        };
        if (colorInputs.Any(input => !IsValidColor(input.Text)))
        {
            MessageBox.Show("Enter a valid color value (for example, #3f7af8) before saving.",
                "Invalid color", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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
        _settings.AppTaskCardBackgroundColor = TaskCardBgInput.Text;
        _settings.AppTaskCardTextColor = TaskCardTextInput.Text;
        _settings.AppTaskCardSubtitleColor = TaskCardSubtitleInput.Text;
        _settings.AppCompletedTaskTextColor = CompletedTaskTextInput.Text;
        _settings.AppTaskCardBorderColor = TaskCardBorderInput.Text;
        _settings.AppCheckboxAccentColor = CheckboxAccentInput.Text;
        _settings.AppFontFamily = AppFontFamilyInput.Text.Trim();
        _settings.WidgetFontFamily = WidgetFontFamilyInput.Text.Trim();

        // Save settings
        _settings.Save();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _settings.CopyFrom(_savedSettings);
        NotifySettingsChanged();
        DialogResult = false;
        Close();
    }

    private void ReturnToDefaults_Click(object sender, RoutedEventArgs e)
    {
        _settings.CopyFrom(new AppSettings());
        LoadControlsFromSettings();
        NotifySettingsChanged();
    }
}
