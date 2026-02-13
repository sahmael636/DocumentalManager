using DocumentalManager.ViewModels;

namespace DocumentalManager.Views;

[QueryProperty(nameof(TableName), "tableName")]
[QueryProperty(nameof(PageId), "id")]
public partial class FormularioPage : ContentPage
{
    public FormularioPage(FormularioViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private string tableName;
    public string TableName
    {
        get => tableName;
        set
        {
            tableName = value;
            if (BindingContext is FormularioViewModel vm && !string.IsNullOrEmpty(value))
            {
                vm.TableName = value;
            }
        }
    }

    // Cambiado de 'Id' a 'PageId' para evitar colisiones/reflection AmbiguousMatchException
    private string pageId;
    public string PageId
    {
        get => pageId;
        set
        {
            pageId = value;
            if (BindingContext is FormularioViewModel vm && int.TryParse(value, out var parsed))
            {
                vm.Id = parsed;
            }
        }
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (BindingContext is FormularioViewModel vm)
        {
            await vm.LoadDataAsync();
        }
    }
}