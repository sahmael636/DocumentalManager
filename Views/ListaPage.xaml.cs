using DocumentalManager.ViewModels;

namespace DocumentalManager.Views;

[QueryProperty(nameof(TableName), "tableName")]
public partial class ListaPage : ContentPage
{
    private ListaViewModel _viewModel;

    private string _tableName;
    public string TableName
    {
        get => _tableName;
        set
        {
            _tableName = value;
            if (_viewModel != null)
            {
                _viewModel.TableName = value;
            }
        }
    }

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
            // TableName ya se asignó automáticamente por el QueryProperty
            await vm.LoadDataAsync();
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.FilterItemsCommand.Execute(null);
        }
    }
}