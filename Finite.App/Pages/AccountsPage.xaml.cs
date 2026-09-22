using System.Windows.Controls;
using Finite.App.ViewModels;

namespace Finite.App.Pages;

public partial class AccountsPage : Page
{
    private readonly AccountsViewModel _viewModel;

    public AccountsPage(AccountsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.LoadCommand.Execute(null);
    }
}
