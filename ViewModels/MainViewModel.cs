using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentalManager.Views;

namespace DocumentalManager.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        public MainViewModel()
        {
            Title = "Gestor Documental";
        }

        [RelayCommand]
        private async Task GoToTablas()
        {
            //await Shell.Current.GoToAsync("//TablasPage"); //Borra la pila de navegación y navega a TablasPage
            //await Shell.Current.GoToAsync("TablasPage");
            await Shell.Current.GoToAsync(nameof(TablasPage));
        }

        [RelayCommand]
        private async Task GoToConsulta()
        {
            //await Shell.Current.GoToAsync("//ConsultaPage"); //Borra la pila de navegación y navega a ConsultaPage
            //await Shell.Current.GoToAsync("ConsultaPage");
            await Shell.Current.GoToAsync(nameof(ConsultaPage));
        }
    }
}