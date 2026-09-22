using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finite.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace Finite.App.ViewModels;

/// <summary>Display wrapper for the theme enum (ComboBox-friendly).</summary>
public record ThemeOption(AppThemeChoice Value, string Label)
{
    public override string ToString() => Label;
}

public partial class SettingsViewModel : ObservableObject
{
    private readonly IServiceProvider _services;

    public SettingsViewModel(IServiceProvider services)
    {
        _services = services;
        ThemeOptions =
        [
            new ThemeOption(AppThemeChoice.Dark, "Dark"),
            new ThemeOption(AppThemeChoice.Light, "Light"),
            new ThemeOption(AppThemeChoice.System, "Match Windows"),
        ];
        _selectedTheme = ThemeOptions.First(t => t.Value == App.CurrentTheme);
    }

    public IReadOnlyList<ThemeOption> ThemeOptions { get; }

    [ObservableProperty]
    private ThemeOption _selectedTheme;

    partial void OnSelectedThemeChanged(ThemeOption value)
    {
        if (value is null) return;
        App.ApplyTheme(value.Value);
    }

    [ObservableProperty] private string _statusText = "";

    public string DatabasePath => DbPaths.GetDatabasePath();
    public string SettingsPath => AppSettings.GetSettingsPath();
    public string AppVersion => typeof(App).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    [RelayCommand]
    private void OpenDatabaseFolder()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{DbPaths.GetDatabaseDirectory()}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            StatusText = $"Could not open folder: {ex.Message}";
        }
    }

    [RelayCommand]
    private void BackupDatabase()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "Backup database",
                FileName = $"finite-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db",
                Filter = "SQLite database (*.db)|*.db|All files (*.*)|*.*",
            };
            if (dialog.ShowDialog() != true) return;

            // Close open connections so the SQLite file isn't mid-write.
            _services.GetRequiredService<FiniteDbContext>().Database.CloseConnection();

            var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            File.Copy(DbPaths.GetDatabasePath(), dialog.FileName, overwrite: true);
            StatusText = $"Backup saved — {stamp}";
        }
        catch (Exception ex)
        {
            StatusText = $"Backup failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RestoreDatabase()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Restore database",
                Filter = "SQLite database (*.db)|*.db|All files (*.*)|*.*",
            };
            if (dialog.ShowDialog() != true) return;

            _services.GetRequiredService<FiniteDbContext>().Database.CloseConnection();

            var target = DbPaths.GetDatabasePath();
            File.Copy(dialog.FileName, target, overwrite: true);
            StatusText = "Database restored — restart the app to load the restored data.";
        }
        catch (Exception ex)
        {
            StatusText = $"Restore failed: {ex.Message}";
        }
    }
}
