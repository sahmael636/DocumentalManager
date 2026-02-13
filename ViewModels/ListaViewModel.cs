using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentalManager.Models;
using DocumentalManager.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DocumentalManager.ViewModels
{
    public partial class ListaViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ExcelService _excelService;

        [ObservableProperty]
        private string tableName;

        [ObservableProperty]
        private ObservableCollection<object> items;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private ObservableCollection<object> filteredItems;

        public ListaViewModel(DatabaseService databaseService, ExcelService excelService)
        {
            _databaseService = databaseService;
            _excelService = excelService;
            Items = new ObservableCollection<object>();
            FilteredItems = new ObservableCollection<object>();
        }

        public async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                Items.Clear();
                var items = await GetItemsFromTable();
                foreach (var item in items)
                {
                    Items.Add(item);
                }
                FilterItems();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<IEnumerable<object>> GetItemsFromTable()
        {
            switch (TableName)
            {
                case "Fondos":
                    return await _databaseService.GetAllAsync<Fondo>();
                case "Subfondos":
                    return await _databaseService.GetAllAsync<Subfondo>();
                case "UnidadesAdministrativas":
                    return await _databaseService.GetAllAsync<UnidadAdministrativa>();
                case "OficinasProductoras":
                    return await _databaseService.GetAllAsync<OficinaProductora>();
                case "Series":
                    return await _databaseService.GetAllAsync<Serie>();
                case "Subseries":
                    return await _databaseService.GetAllAsync<Subserie>();
                case "TiposDocumentales":
                    return await _databaseService.GetAllAsync<TipoDocumental>();
                default:
                    return new List<object>();
            }
        }

        [RelayCommand]
        private void FilterItems()
        {
            FilteredItems.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var item in Items)
                {
                    FilteredItems.Add(item);
                }
            }
            else
            {
                var filtered = Items.Where(item =>
                {
                    var nombre = item.GetType().GetProperty("Nombre")?.GetValue(item)?.ToString() ?? "";
                    var codigo = item.GetType().GetProperty("Codigo")?.GetValue(item)?.ToString() ?? "";

                    return nombre.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                           codigo.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                });

                foreach (var item in filtered)
                {
                    FilteredItems.Add(item);
                }
            }
        }

        [RelayCommand]
        private async Task Nuevo()
        {
            await Shell.Current.GoToAsync($"FormularioPage?tableName={TableName}&id=0");
        }

        [RelayCommand]
        private async Task Editar(object item)
        {
            var id = (int)item.GetType().GetProperty("Id").GetValue(item);
            await Shell.Current.GoToAsync($"FormularioPage?tableName={TableName}&id={id}");
        }

        [RelayCommand]
        private async Task Eliminar(object item)
        {
            var id = (int)item.GetType().GetProperty("Id").GetValue(item);

            // Verificar si tiene registros relacionados
            var hasRelated = await _databaseService.HasRelatedRecordsAsync(TableName.TrimEnd('s'), id);

            if (hasRelated)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    "No se puede eliminar porque tiene registros relacionados.",
                    "OK");
                return;
            }

            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirmar",
                "¿Está seguro de eliminar este registro?",
                "Sí", "No");

            if (confirm)
            {
                await DeleteItem(item);
                await LoadDataAsync();
            }
        }

        private async Task DeleteItem(object item)
        {
            switch (TableName)
            {
                case "Fondos":
                    await _databaseService.DeleteAsync((Fondo)item);
                    break;
                case "Subfondos":
                    await _databaseService.DeleteAsync((Subfondo)item);
                    break;
                case "UnidadesAdministrativas":
                    await _databaseService.DeleteAsync((UnidadAdministrativa)item);
                    break;
                case "OficinasProductoras":
                    await _databaseService.DeleteAsync((OficinaProductora)item);
                    break;
                case "Series":
                    await _databaseService.DeleteAsync((Serie)item);
                    break;
                case "Subseries":
                    await _databaseService.DeleteAsync((Subserie)item);
                    break;
                case "TiposDocumentales":
                    await _databaseService.DeleteAsync((TipoDocumental)item);
                    break;
            }
        }

        [RelayCommand]
        private async Task Importar()
        {
            try
            {
                var options = new PickOptions
                {
                    PickerTitle = "Seleccione archivo Excel",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".xlsx", ".xls" } },
                        { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel" } }
                    })
                };

                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    // Aquí implementar la lógica específica de importación para cada tabla
                    await Application.Current.MainPage.DisplayAlert(
                        "Importación",
                        "Funcionalidad en desarrollo",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task Exportar()
        {
            try
            {
                var filePath = Path.Combine(FileSystem.CacheDirectory, $"{TableName}_{DateTime.Now:yyyyMMddHHmmss}.xlsx");

                // Aquí implementar la lógica específica de exportación para cada tabla
                await Application.Current.MainPage.DisplayAlert(
                    "Exportación",
                    $"Archivo guardado en: {filePath}",
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}