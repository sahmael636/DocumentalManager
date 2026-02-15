using DocumentalManager.ViewModels;
using Microsoft.Maui.Controls;

namespace DocumentalManager.Views;

public partial class ConsultaPage : ContentPage
{
    public ConsultaPage(ConsultaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // Maneja el evento Navigating del Shell — usa ShellNavigatingEventArgs en .NET MAUI
    private async void OnNavigatingBack(object sender, ShellNavigatingEventArgs e)
    {
        // Cancelar la navegación predeterminada y forzar regreso a la raíz
        e.Cancel();
        await Shell.Current.GoToAsync("//");
    }
}