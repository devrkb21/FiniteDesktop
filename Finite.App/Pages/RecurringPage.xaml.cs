using System.Windows.Controls;
using Finite.App.ViewModels;

namespace Finite.App.Pages;

public partial class RecurringPage : Page
{
    private readonly RecurringViewModel _viewModel;

    public RecurringPage(RecurringViewModel viewModel)
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
