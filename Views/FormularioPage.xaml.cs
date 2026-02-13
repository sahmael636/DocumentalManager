using DocumentalManager.ViewModels;

namespace DocumentalManager.Views;

public partial class FormularioPage : ContentPage
{
    public FormularioPage(FormularioViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (BindingContext is FormularioViewModel vm)
        {
            // Obtener parámetros
            if (Shell.Current?.CurrentPage?.BindingContext is FormularioViewModel fvm)
            {
                var tableName = Shell.Current.CurrentPage?.BindingContext?.GetType()
                    .GetProperty("TableName")?.GetValue(Shell.Current.CurrentPage.BindingContext)?.ToString();

                var id = Shell.Current.CurrentPage?.BindingContext?.GetType()
                    .GetProperty("Id")?.GetValue(Shell.Current.CurrentPage.BindingContext);

                if (!string.IsNullOrEmpty(tableName))
                {
                    vm.TableName = tableName;
                }

                if (id != null)
                {
                    vm.Id = (int)id;
                }
            }

            await vm.LoadDataAsync();
        }
    }
}