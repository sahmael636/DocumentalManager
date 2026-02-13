using DocumentalManager.ViewModels;

namespace DocumentalManager.Views;

public partial class ListaPage : ContentPage
{
    private ListaViewModel _viewModel;

    public ListaPage(ListaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (BindingContext is ListaViewModel vm)
        {
            vm.TableName = GetTableName();
            await vm.LoadDataAsync();
        }
    }

    private string GetTableName()
    {
        if (Shell.Current?.CurrentItem?.CurrentItem is ShellContent shellContent)
        {
            var route = shellContent.Route;
            var parameters = Shell.Current.CurrentPage?.BindingContext?.GetType()
                .GetProperty("TableName")?.GetValue(Shell.Current.CurrentPage.BindingContext);

            return parameters?.ToString() ?? "Fondos";
        }

        // Obtener de query parameters
        if (Shell.Current?.CurrentPage?.BindingContext is ListaViewModel vm)
        {
            return vm.TableName;
        }

        return "Fondos";
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.FilterItemsCommand.Execute(null);
        }
    }
}