using System.Text.Json;
using System.IO;

namespace Tasker;

public class AppSettings
{
    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    public void CopyFrom(AppSettings source)
    {
        foreach (var property in typeof(AppSettings).GetProperties())
            if (property.CanWrite)
                property.SetValue(this, property.GetValue(source));
    }

    // Widget Settings
    public double WidgetOpacity { get; set; } = 1.0;
    public int WidgetPositionX { get; set; } = 20;
    public int WidgetPositionY { get; set; } = 20;
    public string WidgetTitleColor { get; set; } = "#F8F7FF";
    public string WidgetSubtitleColor { get; set; } = "#BFC9F5";
    public string WidgetDescriptionColor { get; set; } = "#BFC9F5";
    public int WidgetTitleFontSize { get; set; } = 16;
    public int WidgetSubtitleFontSize { get; set; } = 11;
    public int WidgetDescriptionFontSize { get; set; } = 12;
    public int WidgetMaxWidth { get; set; } = 300;
    public string WidgetBackgroundColor { get; set; } = "#171A3B";
    public string WidgetBorderColor { get; set; } = "#6157C8";

    // Application Colors
    public string AppBackgroundColor { get; set; } = "#11152E";
    public string AppPanelColor { get; set; } = "#20264B";
    public string AppTextColor { get; set; } = "#F4F5FF";
    public string AppSecondaryTextColor { get; set; } = "#B7C0E8";
    public string AppBorderColor { get; set; } = "#414A7C";
    public string AppPrimaryButtonColor { get; set; } = "#8067F2";
    public string AppPrimaryButtonTextColor { get; set; } = "#ffffff";
    public string AppSecondaryButtonColor { get; set; } = "#343C6A";
    public string AppSecondaryButtonTextColor { get; set; } = "#E6E9FF";
    public string AppTaskCardBackgroundColor { get; set; } = "#2A315D";
    public string AppTaskCardTextColor { get; set; } = "";
    public string AppTaskCardSubtitleColor { get; set; } = "";
    public string AppCompletedTaskTextColor { get; set; } = "#A4AED6";
    public string AppTaskCardBorderColor { get; set; } = "";
    public string AppCheckboxAccentColor { get; set; } = "";

    // Application Fonts
    public int AppTitleFontSize { get; set; } = 24;
    public int AppHeadingFontSize { get; set; } = 14;
    public int AppLabelFontSize { get; set; } = 12;
    public int AppTextFontSize { get; set; } = 14;
    public int AppSmallTextFontSize { get; set; } = 11;
    public string AppFontFamily { get; set; } = "Segoe UI";
    public string WidgetFontFamily { get; set; } = "Segoe UI";

    // Theme
    public string ThemePrimary { get; set; } = "#8067F2";
    public string ThemeSecondary { get; set; } = "#171A3B";

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
