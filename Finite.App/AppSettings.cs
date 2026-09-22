using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Finite.App;

/// <summary>Which visual theme the app uses.</summary>
public enum AppThemeChoice
{
    Dark,
    Light,
    /// <summary>Follow the Windows personalization setting.</summary>
    System,
}

/// <summary>
/// Persisted user settings (theme choice), stored as JSON in
/// %APPDATA%\FiniteDesktop\settings.json. Tolerates missing/corrupt files.
/// </summary>
public class AppSettings
{
    [JsonPropertyName("theme")]
    public AppThemeChoice Theme { get; set; } = AppThemeChoice.Dark;

    public static AppSettings Load()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path)) return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
        }
        catch (Exception)
        {
            // Corrupt settings file → defaults, never crash the app on startup.
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception)
        {
            // Best-effort persistence; a read-only disk shouldn't take the app down.
        }
    }

    public static string GetSettingsPath()
        => Path.Combine(DbPaths.GetDatabaseDirectory(), "settings.json");
}
