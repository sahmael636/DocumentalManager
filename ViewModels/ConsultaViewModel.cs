using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentalManager.Models;
using DocumentalManager.Services;
using System.Collections.ObjectModel;

namespace DocumentalManager.ViewModels
{
    public partial class ConsultaViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private ObservableCollection<BusquedaResultado> resultados;

        public ConsultaViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            Resultados = new ObservableCollection<BusquedaResultado>();
            Title = "Consulta Documental";
        }

        [RelayCommand]
        private async Task Buscar()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await Application.Current.MainPage.DisplayAlert("Aviso", "Ingrese un texto para buscar", "OK");
                return;
            }

            IsBusy = true;
            Resultados.Clear();

            try
            {
                var results = await _databaseService.BuscarPorTexto(SearchText);
                foreach (var result in results)
                {
                    Resultados.Add(result);
                }

                if (Resultados.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "No se encontraron resultados", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Comando para el Toolbar "Atrás"
        [RelayCommand]
        private async Task Volver()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch
            {
                // Silenciar por ahora; opcional: mostrar aviso
            }
        }
    }
}