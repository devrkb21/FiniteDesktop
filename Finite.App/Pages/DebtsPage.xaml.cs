using System.Windows.Controls;
using Finite.App.ViewModels;

namespace Finite.App.Pages;

public partial class DebtsPage : Page
{
    private readonly DebtsViewModel _viewModel;

    public DebtsPage(DebtsViewModel viewModel)
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
