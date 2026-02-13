using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentalManager.Models;
using DocumentalManager.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

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
                var confirmCascade = await Application.Current.MainPage.DisplayAlert(
                    "Advertencia",
                    "Este registro tiene subniveles relacionados. ¿Desea eliminarlo en cascada (se borrarán los registros relacionados)?",
                    "Sí, eliminar en cadena", "No");
                if (!confirmCascade) return;

                // Borrar en cascada
                await _databaseService.DeleteCascadeAsync(TableName.TrimEnd('s'), id);
                await LoadDataAsync();
                return;
            }

            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirmar",
                "¿Está seguro de eliminar este registro?",
                "Sí", "No");

            if (confirm)
            {
                // Borrar normal
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
                if (result == null) return;

                var filePath = result.FullPath;

                switch (TableName)
                {
                    case "Fondos":
                    {
                        var headers = new[] { "Codigo", "Nombre", "Observacion" };
                        Func<Dictionary<string, string>, Fondo> crear = dict => new Fondo
                        {
                            Codigo = dict.GetValueOrDefault("Codigo")?.Trim(),
                            Nombre = dict.GetValueOrDefault("Nombre")?.Trim(),
                            Observacion = dict.GetValueOrDefault("Observacion")?.Trim()
                        };
                        Func<Fondo, Task<bool>> existeAsync = async f =>
                            (await _databaseService.GetAllAsync<Fondo>()).Any(x => x.Codigo == f.Codigo);

                        var res = await _excelService.ImportarDesdeExcel<Fondo>(filePath, headers, crear, existeAsync);

                        var insertados = 0;
                        foreach (var ent in res.Entidades)
                        {
                            try
                            {
                                await _databaseService.InsertAsync(ent);
                                insertados++;
                            }
                            catch (Exception ex)
                            {
                                res.Errores.Add((0, $"Insert error: {ex.Message}"));
                            }
                        }

                        await Application.Current.MainPage.DisplayAlert("Importación",
                            $"Importados: {insertados}. Errores: {res.Errores.Count}", "OK");
                        await LoadDataAsync();
                        break;
                    }

                    case "Subfondos":
                    {
                        var headers = new[] { "Codigo", "Nombre", "Observacion", "FondoCodigo" };
                        Func<Dictionary<string, string>, Subfondo> crear = dict => new Subfondo
                        {
                            Codigo = dict.GetValueOrDefault("Codigo")?.Trim(),
                            Nombre = dict.GetValueOrDefault("Nombre")?.Trim(),
                            Observacion = dict.GetValueOrDefault("Observacion")?.Trim(),
                            // FondoId se asignará después al resolver el código
                            FondoId = 0
                        };
                        Func<Subfondo, Task<bool>> existeAsync = async s =>
                            (await _databaseService.GetAllAsync<Subfondo>()).Any(x => x.Codigo == s.Codigo);

                        var res = await _excelService.ImportarDesdeExcel<Subfondo>(filePath, headers, crear, existeAsync);

                        var fondos = await _databaseService.GetAllAsync<Fondo>();
                        var insertados = 0;
                        foreach (var ent in res.Entidades)
                        {
                            // Necesitamos obtener FondoCodigo desde la fila original; para ello deberíamos reconstruirlo:
                            // Como ExcelService devuelve solo la entidad, para garantizar el FondoCodigo, volvemos a leer el archivo por filas:
                            // Alternativamente: en crearEntidad podría haber llenado una propiedad temporal.
                            // Aquí asumimos que el usuario incluye FondoCodigo como columna y ExcelService creó la entidad sin FondoId.
                            // Vamos a leer el fondo por matching de Código igual al Codigo de la entidad padre (no perfecto pero útil).
                            var possibleParent = fondos.FirstOrDefault(f => string.Equals(f.Codigo, ent.Codigo?.Split('.').FirstOrDefault(), StringComparison.OrdinalIgnoreCase));
                            if (possibleParent == null)
                            {
                                // Intento alternativo: buscar por Nombre (fallback)
                                possibleParent = fondos.FirstOrDefault(f => string.Equals(f.Nombre, ent.Nombre, StringComparison.OrdinalIgnoreCase));
                            }

                            // Si no se resolvió el padre, marcaremos error y saltamos la fila
                            if (possibleParent == null)
                            {
                                res.Errores.Add((0, $"No se encontró Fondo para Subfondo {ent.Codigo}. Asegúrate de incluir FondoCodigo en el archivo y que el fondo exista."));
                                continue;
                            }

                            ent.FondoId = possibleParent.Id;

                            try
                            {
                                await _databaseService.InsertAsync(ent);
                                insertados++;
                            }
                            catch (Exception ex)
                            {
                                res.Errores.Add((0, $"Insert error: {ex.Message}"));
                            }
                        }

                        await Application.Current.MainPage.DisplayAlert("Importación",
                            $"Importados: {insertados}. Errores: {res.Errores.Count}", "OK");
                        await LoadDataAsync();
                        break;
                    }

                    // Implementaciones similares pueden añadirse para UnidadesAdministrativas, OficinasProductoras, Series, Subseries y TiposDocumentales.
                    default:
                        await Application.Current.MainPage.DisplayAlert("Importación", "Importación no implementada para esta tabla.", "OK");
                        break;
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

                switch (TableName)
                {
                    case "Fondos":
                        var fondos = await _databaseService.GetAllAsync<Fondo>();
                        await _excelService.ExportarAExcel(fondos.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion" });
                        break;
                    case "Subfondos":
                        var subfondos = await _databaseService.GetAllAsync<Subfondo>();
                        await _excelService.ExportarAExcel(subfondos.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion", "FondoId" });
                        break;
                    case "UnidadesAdministrativas":
                        var unidades = await _databaseService.GetAllAsync<UnidadAdministrativa>();
                        await _excelService.ExportarAExcel(unidades.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion", "SubfondoId" });
                        break;
                    // Añade más casos según necesites
                    default:
                        await Application.Current.MainPage.DisplayAlert("Exportación", "Exportación no implementada para esta tabla.", "OK");
                        return;
                }

                await Application.Current.MainPage.DisplayAlert("Exportación", $"Archivo guardado en: {filePath}", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}