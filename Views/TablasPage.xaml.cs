using DocumentalManager.ViewModels;

namespace DocumentalManager.Views;

public partial class TablasPage : ContentPage
{
    public TablasPage(TablasViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
