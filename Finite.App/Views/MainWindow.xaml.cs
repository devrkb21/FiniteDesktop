using System.Windows;
using Finite.App.Pages;
using Wpf.Ui.Controls;

namespace Finite.App.Views;

/// <summary>
/// Main shell window. Pages are resolved from the app's DI container through
/// NavigationPageProvider so they receive services via constructor injection.
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        RootNavigation.SetPageService(new NavigationPageProvider(App.Services));
        Loaded += (_, _) => RootNavigation.Navigate(typeof(DashboardPage));
    }
}
