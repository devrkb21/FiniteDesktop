using System.Windows.Controls;

namespace Finite.App.Pages;

/// <summary>Settings shell — all logic lives in SettingsViewModel.</summary>
public partial class SettingsPage : Page
{
    public SettingsPage(ViewModels.SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
