using System.Text.Json;
using System.IO;

namespace Tasker;

public class AppSettings
{
    // Widget Settings
    public double WidgetOpacity { get; set; } = 1.0;
    public int WidgetPositionX { get; set; } = 20;
    public int WidgetPositionY { get; set; } = 20;
    public string WidgetTitleColor { get; set; } = "#FFFFFF";
    public string WidgetSubtitleColor { get; set; } = "#DFEAFC";
    public string WidgetDescriptionColor { get; set; } = "#DFEAFC";
    public int WidgetTitleFontSize { get; set; } = 16;
    public int WidgetSubtitleFontSize { get; set; } = 11;
    public int WidgetDescriptionFontSize { get; set; } = 12;
    public int WidgetMaxWidth { get; set; } = 300;
    public string WidgetBackgroundColor { get; set; } = "#1f2b44";
    public string WidgetBorderColor { get; set; } = "#dfeafc";

    // Application Colors
    public string AppBackgroundColor { get; set; } = "#f4f6fb";
    public string AppPanelColor { get; set; } = "#ffffff";
    public string AppTextColor { get; set; } = "#1d2433";
    public string AppSecondaryTextColor { get; set; } = "#5e6a81";
    public string AppBorderColor { get; set; } = "#dde4f2";
    public string AppPrimaryButtonColor { get; set; } = "#3f7af8";
    public string AppPrimaryButtonTextColor { get; set; } = "#ffffff";
    public string AppSecondaryButtonColor { get; set; } = "#e0e0e0";
    public string AppSecondaryButtonTextColor { get; set; } = "#333333";

    // Application Fonts
    public int AppTitleFontSize { get; set; } = 24;
    public int AppHeadingFontSize { get; set; } = 14;
    public int AppLabelFontSize { get; set; } = 12;
    public int AppTextFontSize { get; set; } = 14;
    public int AppSmallTextFontSize { get; set; } = 11;

    // Theme
    public string ThemePrimary { get; set; } = "#3f7af8";
    public string ThemeSecondary { get; set; } = "#1d2433";

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Tasker",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail if we can't save settings
        }
    }
}
