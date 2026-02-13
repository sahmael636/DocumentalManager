using DocumentalManager.ViewModels;

namespace DocumentalManager.Views;

public partial class ConsultaPage : ContentPage
{
    public ConsultaPage(ConsultaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}