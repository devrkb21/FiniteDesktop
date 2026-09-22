using System.Windows;
using Finite.Data;
using Finite.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Finite.App;

public partial class App : System.Windows.Application
{
    private static IHost? _host;

    public static IServiceProvider Services => (_host ?? throw new InvalidOperationException("Host not started")).Services;

    private static AppSettings _settings = new();

    /// <summary>The theme the user last chose (persisted).</summary>
    public static AppThemeChoice CurrentTheme => _settings.Theme;

    /// <summary>Apply a theme now and remember the choice.</summary>
    public static void ApplyTheme(AppThemeChoice choice)
    {
        _settings.Theme = choice;
        _settings.Save();
        var theme = choice switch
        {
            AppThemeChoice.Light => Wpf.Ui.Appearance.ApplicationTheme.Light,
            AppThemeChoice.System => WindowsPrefersLight()
                ? Wpf.Ui.Appearance.ApplicationTheme.Light
                : Wpf.Ui.Appearance.ApplicationTheme.Dark,
            _ => Wpf.Ui.Appearance.ApplicationTheme.Dark,
        };
        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(theme);
    }

    private static bool WindowsPrefersLight()
    {
        var val = Microsoft.Win32.Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme", 1);
        return val is int i && i == 1;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _settings = AppSettings.Load();
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                var dbPath = DbPaths.GetDatabasePath();
                services.AddDbContext<FiniteDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));

                services.AddScoped<BalanceService>();
                services.AddScoped<AccountService>();
                services.AddScoped<TransactionService>();
                services.AddScoped<TransferService>();
                services.AddScoped<DebtService>();
                services.AddScoped<BudgetService>();
                services.AddScoped<BudgetEvaluator>();
                services.AddScoped<RecurrenceEngine>();
                services.AddScoped<RecurringTransactionService>();
                services.AddScoped<CategoryService>();
                services.AddScoped<SavingsGoalService>();
                services.AddScoped<ReportService>();

                services.AddSingleton<NavigationPageProvider>();
                services.AddSingleton<Views.MainWindow>();

                // ViewModels (pages receive them via constructor injection)
                services.AddTransient<ViewModels.DashboardViewModel>();
                services.AddTransient<ViewModels.AccountsViewModel>();
                services.AddTransient<ViewModels.TransactionsViewModel>();
                services.AddTransient<ViewModels.TransfersViewModel>();
                services.AddTransient<ViewModels.BudgetsViewModel>();
                services.AddTransient<ViewModels.DebtsViewModel>();
                services.AddTransient<ViewModels.SavingsGoalsViewModel>();
                services.AddTransient<ViewModels.RecurringViewModel>();
                services.AddTransient<ViewModels.CategoriesViewModel>();
                services.AddTransient<ViewModels.AnalyticsViewModel>();
                services.AddTransient<ViewModels.BillCalendarViewModel>();
                services.AddTransient<ViewModels.SettingsViewModel>();

                services.AddTransient<Pages.DashboardPage>();
                services.AddTransient<Pages.TransactionsPage>();
                services.AddTransient<Pages.AccountsPage>();
                services.AddTransient<Pages.TransfersPage>();
                services.AddTransient<Pages.BudgetsPage>();
                services.AddTransient<Pages.DebtsPage>();
                services.AddTransient<Pages.SavingsGoalsPage>();
                services.AddTransient<Pages.RecurringPage>();
                services.AddTransient<Pages.CategoriesPage>();
                services.AddTransient<Pages.AnalyticsPage>();
                services.AddTransient<Pages.BillCalendarPage>();
                services.AddTransient<Pages.SettingsPage>();
            })
            .Build();

        using (var scope = _host.Services.CreateScope())
        {
            DbInitializer.Initialize(scope.ServiceProvider.GetRequiredService<FiniteDbContext>());
        }

        _host.Start();
        ApplyTheme(_settings.Theme); // persisted choice (default Dark)
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, "Finite — Unexpected Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
