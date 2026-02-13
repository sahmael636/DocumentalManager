using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
            await Shell.Current.GoToAsync("//TablasPage");
        }

        [RelayCommand]
        private async Task GoToConsulta()
        {
            await Shell.Current.GoToAsync("//ConsultaPage");
        }
    }
}