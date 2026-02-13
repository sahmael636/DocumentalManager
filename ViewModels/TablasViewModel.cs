using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DocumentalManager.ViewModels
{
    public partial class TablasViewModel : BaseViewModel
    {
        public TablasViewModel()
        {
            Title = "Administración de Tablas";
        }

        [RelayCommand]
        private async Task NavigateToTable(string tableName)
        {
            await Shell.Current.GoToAsync($"ListaPage?tableName={tableName}");
        }
    }
}