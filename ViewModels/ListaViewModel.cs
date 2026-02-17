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
using System.IO;

namespace DocumentalManager.ViewModels
{
    public partial class ListaViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ExcelService _excelService;

        [ObservableProperty]
        private string tableName;

        [ObservableProperty] private string parentId;
        [ObservableProperty] private string parentKey;

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
        private List<object> ApplyParentFilter<T>(List<T> items) where T : class
        {
            if (string.IsNullOrWhiteSpace(ParentId) || string.IsNullOrWhiteSpace(ParentKey))
                return items.Cast<object>().ToList();

            var prop = typeof(T).GetProperty(ParentKey);
            if (prop == null)
                return items.Cast<object>().ToList(); // si no existe, no filtra

            return items
                .Where(x => (prop.GetValue(x)?.ToString() ?? "") == ParentId)
                .Cast<object>()
                .ToList();
        }
        private async Task<IEnumerable<object>> GetItemsFromTable()
        {
            switch (TableName)
            {
                case "Fondos":
                    return await _databaseService.GetAllAsync<Fondo>();

                case "Subfondos":
                    //return await _databaseService.GetAllAsync<Subfondo>();
                    var all = await _databaseService.GetAllAsync<Subfondo>();
                    return ApplyParentFilter(all);
                case "UnidadesAdministrativas":
                    //return await _databaseService.GetAllAsync<UnidadAdministrativa>();
                    var all2 = await _databaseService.GetAllAsync<UnidadAdministrativa>();
                    return ApplyParentFilter(all2);
                case "OficinasProductoras":
                    //return await _databaseService.GetAllAsync<OficinaProductora>();
                    var all3 = await _databaseService.GetAllAsync<OficinaProductora>();
                    return ApplyParentFilter(all3);
                case "Series":
                    //return await _databaseService.GetAllAsync<Serie>();
                    var all4 = await _databaseService.GetAllAsync<Serie>();
                    return ApplyParentFilter(all4);
                case "Subseries":
                    //return await _databaseService.GetAllAsync<Subserie>();
                    var all5 = await _databaseService.GetAllAsync<Subserie>();
                    return ApplyParentFilter(all5);
                case "TiposDocumentales":
                    //return await _databaseService.GetAllAsync<TipoDocumental>();
                    var all6 = await _databaseService.GetAllAsync<TipoDocumental>();
                    return ApplyParentFilter(all6);
                default:
                    return new List<object>();
            }
        }

        [RelayCommand]
        private async Task VerHijos(object item)
        {
            if (item == null) return;

            // toma el Id del item actual (tus entidades tienen Id string)
            var id = item.GetType().GetProperty("Id")?.GetValue(item)?.ToString();
            if (string.IsNullOrWhiteSpace(id)) return;

            // define siguiente tabla + clave FK según la tabla actual
            string nextTable = null;
            string nextParentKey = null;

            switch (TableName)
            {
                case "Fondos":
                    nextTable = "Subfondos";
                    nextParentKey = "FondoId";
                    break;

                case "Subfondos":
                    nextTable = "UnidadesAdministrativas";
                    nextParentKey = "SubfondoId";
                    break;

                case "UnidadesAdministrativas":
                    nextTable = "OficinasProductoras";
                    nextParentKey = "UnidadAdministrativaId";
                    break;

                case "OficinasProductoras":
                    nextTable = "Series";
                    nextParentKey = "OficinaProductoraId";
                    break;

                case "Series":
                    nextTable = "Subseries";
                    nextParentKey = "SerieId";
                    break;

                case "Subseries":
                    nextTable = "TiposDocumentales";
                    nextParentKey = "SubserieId";
                    break;
            }

            if (nextTable == null) return;

            await Shell.Current.GoToAsync(
                $"ListaPage?tableName={nextTable}&parentId={Uri.EscapeDataString(id)}&parentKey={Uri.EscapeDataString(nextParentKey)}"
            );
        }

        [RelayCommand]
        private async Task VerDetalle(object item)
        {
            if (item == null) return;

            var id = item.GetType().GetProperty("Id")?.GetValue(item)?.ToString();
            if (string.IsNullOrWhiteSpace(id)) return;

            await Shell.Current.GoToAsync(
                $"FormularioPage?tableName={TableName}&id={Uri.EscapeDataString(id)}&readOnly=true"
            );
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
            // Navegar para creación: pasar id vacío (o "new") — ajusta FormularioViewModel para aceptar string id
            await Shell.Current.GoToAsync($"FormularioPage?tableName={TableName}&id=new");
        }

        [RelayCommand]
        private async Task Editar(object item)
        {
            // Id ahora es string (GUID)
            var id = item.GetType().GetProperty("Id")?.GetValue(item)?.ToString() ?? string.Empty;
            await Shell.Current.GoToAsync($"FormularioPage?tableName={TableName}&id={id}");
        }

        [RelayCommand]
        private async Task Eliminar(object item)
        {
            // Id ahora es string (GUID)
            var id = item.GetType().GetProperty("Id")?.GetValue(item)?.ToString() ?? string.Empty;

            // Verificar si tiene registros relacionados (firma espera string)
            var entityName = EntityNameMap[TableName];
            //var hasRelated = await _databaseService.HasRelatedRecordsAsync(TableName.TrimEnd('s'), id);
            var hasRelated = await _databaseService.HasRelatedRecordsAsync(entityName, id);

            if (hasRelated)
            {
                var confirmCascade = await Application.Current.MainPage.DisplayAlert(
                    "Advertencia",
                    "Este registro tiene subniveles relacionados. ¿Desea eliminarlo en cascada (se borrarán los registros relacionados)?",
                    "Sí, eliminar en cadena", "No");
                if (!confirmCascade) return;

                // Borrar en cascada (firma espera string)
                //await _databaseService.DeleteCascadeAsync(TableName.TrimEnd('s'), id);
                entityName = EntityNameMap[TableName];
                await _databaseService.DeleteCascadeAsync(entityName, id);
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

        private static readonly Dictionary<string, string> EntityNameMap = new()
        {
            ["Fondos"] = "Fondo",
            ["Subfondos"] = "Subfondo",
            ["UnidadesAdministrativas"] = "UnidadAdministrativa",
            ["OficinasProductoras"] = "OficinaProductora",
            ["Series"] = "Serie",
            ["Subseries"] = "Subserie",
            ["TiposDocumentales"] = "TipoDocumental",
        };

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

                // Helpers locales: cambios mínimos, sin tocar servicios
                string GetVal(Dictionary<string, string> d, string k)
                    => (d.GetValueOrDefault(k) ?? string.Empty).Trim();

                string GetIdOrNew(Dictionary<string, string> d, string k)
                {
                    var v = GetVal(d, k);
                    return string.IsNullOrWhiteSpace(v) ? Guid.NewGuid().ToString() : v;
                }

                bool ParseBool(Dictionary<string, string> d, string k)
                {
                    var v = GetVal(d, k);
                    return bool.TryParse(v, out var b) && b;
                }

                int ParseInt(Dictionary<string, string> d, string k)
                {
                    var v = GetVal(d, k);
                    return int.TryParse(v, out var n) ? n : 0;
                }

                async Task<bool> ExistsByIdAsync<T>(string id) where T : class, new()
                {
                    if (string.IsNullOrWhiteSpace(id)) return false;
                    var existing = await _databaseService.GetByIdAsync<T>(id);
                    return existing != null;
                }

                // ✅ IMPORTANTE: leer SIEMPRE la hoja que coincide con la tabla actual
                var sheetName = TableName;

                switch (TableName)
                {
                    case "Fondos":
                        {
                            var headers = new[] { "Id", "Codigo", "Nombre", "Observacion" };

                            Func<Dictionary<string, string>, Fondo> crear = dict => new Fondo
                            {
                                Id = GetIdOrNew(dict, "Id"),
                                Codigo = GetVal(dict, "Codigo"),
                                Nombre = GetVal(dict, "Nombre"),
                                Observacion = GetVal(dict, "Observacion")
                            };

                            Func<Fondo, Task<bool>> existeAsync =
                                //async f => await _databaseService.ExistsByCodigoAsync<Fondo>(f.Codigo?.Trim());
                                async f => await ExistsByIdAsync<Fondo>(f.Id);

                            var res = await _excelService.ImportarDesdeExcel<Fondo>(
                                filePath, headers, crear, existeAsync, sheetName);

                            var insertados = 0;
                            for (int i = 0; i < res.Entidades.Count; i++)
                            {
                                var ent = res.Entidades[i];
                                try
                                {
                                    await _databaseService.InsertAsync(ent);
                                    insertados++;
                                }
                                catch (Exception ex)
                                {
                                    res.Errores.Add((i + 2, $"Insert error: {ex.Message}"));
                                    await Application.Current.MainPage.DisplayAlert("Error al insertar", $"Fila {i + 2}: {ex.Message}", "OK");
                                }
                            }

                            await Application.Current.MainPage.DisplayAlert("Importación",
                                $"Detectados: {res.Importados}. Insertados: {insertados}. Errores: {res.Errores.Count}", "OK");
                            await LoadDataAsync();
                            break;
                        }

                    case "Subfondos":
                        {
                            var fondosExist = (await _databaseService.GetAllAsync<Fondo>()).Any();
                            if (!fondosExist)
                            {
                                await Application.Current.MainPage.DisplayAlert("Importación",
                                    "No existen Fondos en la base de datos. Importe primero los Fondos antes de importar Subfondos.", "OK");
                                return;
                            }

                            var headers = new[] { "Id", "Codigo", "Nombre", "Observacion", "FondoId" };

                            Func<Dictionary<string, string>, Subfondo> crear = dict => new Subfondo
                            {
                                Id = GetIdOrNew(dict, "Id"),
                                Codigo = GetVal(dict, "Codigo"),
                                Nombre = GetVal(dict, "Nombre"),
                                Observacion = GetVal(dict, "Observacion"),
                                FondoId = GetVal(dict, "FondoId")
                            };

                            Func<Subfondo, Task<bool>> existeAsync =
                                //async s => await _databaseService.ExistsByCodigoAsync<Subfondo>(s.Codigo?.Trim());
                                async s => await ExistsByIdAsync<Subfondo>(s.Id);

                            var res = await _excelService.ImportarDesdeExcel<Subfondo>(
                                filePath, headers, crear, existeAsync, sheetName);

                            var fondos = await _databaseService.GetAllAsync<Fondo>();
                            var insertados = 0;

                            for (int i = 0; i < res.Entidades.Count; i++)
                            {
                                var ent = res.Entidades[i];
                                var fila = res.Filas[i];

                                var fondoId = GetVal(fila, "FondoId");
                                if (!fondos.Any(f => f.Id == fondoId))
                                {
                                    res.Errores.Add((i + 2, $"No se encontró Fondo para FondoId='{fondoId}'"));
                                    continue;
                                }

                                try
                                {
                                    await _databaseService.InsertAsync(ent);
                                    insertados++;
                                }
                                catch (Exception ex)
                                {
                                    res.Errores.Add((i + 2, $"Insert error: {ex.Message}"));
                                    await Application.Current.MainPage.DisplayAlert("Error al insertar", $"Fila {i + 2}: {ex.Message}", "OK");
                                }
                            }

                            await Application.Current.MainPage.DisplayAlert("Importación",
                                $"Detectados: {res.Importados}. Insertados: {insertados}. Errores: {res.Errores.Count}", "OK");
                            await LoadDataAsync();
                            break;
                        }

                    case "UnidadesAdministrativas":
                        {
                            var subfondosExist = (await _databaseService.GetAllAsync<Subfondo>()).Any();
                            if (!subfondosExist)
                            {
                                await Application.Current.MainPage.DisplayAlert("Importación",
                                    "No existen Subfondos en la base de datos. Importe primero los Subfondos antes de importar Unidades Administrativas.", "OK");
                                return;
                            }

                            var headers = new[] { "Id", "Codigo", "Nombre", "Observacion", "SubfondoId" };

                            Func<Dictionary<string, string>, UnidadAdministrativa> crear = dict => new UnidadAdministrativa
                            {
                                Id = GetIdOrNew(dict, "Id"),
                                Codigo = GetVal(dict, "Codigo"),
                                Nombre = GetVal(dict, "Nombre"),
                                Observacion = GetVal(dict, "Observacion"),
                                SubfondoId = GetVal(dict, "SubfondoId")
                            };

                            Func<UnidadAdministrativa, Task<bool>> existeAsync =
                                //async u => (await _databaseService.GetAllAsync<UnidadAdministrativa>()).Any(x => x.Codigo == u.Codigo);
                                async u => await ExistsByIdAsync<UnidadAdministrativa>(u.Id);

                            var res = await _excelService.ImportarDesdeExcel<UnidadAdministrativa>(
                                filePath, headers, crear, existeAsync, sheetName);

                            var subfondos = await _databaseService.GetAllAsync<Subfondo>();
                            var insertados = 0;

                            for (int i = 0; i < res.Entidades.Count; i++)
                            {
                                var ent = res.Entidades[i];
                                var fila = res.Filas[i];

                                var subfondoId = GetVal(fila, "SubfondoId");
                                if (!subfondos.Any(s => s.Id == subfondoId))
                                {
                                    res.Errores.Add((i + 2, $"No se encontró Subfondo para SubfondoId='{subfondoId}'"));
                                    continue;
                                }

                                try
                                {
                                    await _databaseService.InsertAsync(ent);
                                    insertados++;
                                }
                                catch (Exception ex)
                                {
                                    res.Errores.Add((i + 2, $"Insert error: {ex.Message}"));
                                }
                            }

                            await Application.Current.MainPage.DisplayAlert("Importación",
                                $"Detectados: {res.Importados}. Insertados: {insertados}. Errores: {res.Errores.Count}", "OK");
                            await LoadDataAsync();
                            break;
                        }

                    case "OficinasProductoras":
                        {
                            var unidadesExist = (await _databaseService.GetAllAsync<UnidadAdministrativa>()).Any();
                            if (!unidadesExist)
                            {
                                await Application.Current.MainPage.DisplayAlert("Importación",
                                    "No existen Unidades Administrativas en la base de datos. Importe primero esas antes de las Oficinas Productoras.", "OK");
                                return;
                            }

                            var headers = new[] { "Id", "Codigo", "Nombre", "Observacion", "UnidadAdministrativaId" };

                            Func<Dictionary<string, string>, OficinaProductora> crear = dict => new OficinaProductora
                            {
                                Id = GetIdOrNew(dict, "Id"),
                                Codigo = GetVal(dict, "Codigo"),
                                Nombre = GetVal(dict, "Nombre"),
                                Observacion = GetVal(dict, "Observacion"),
                                UnidadAdministrativaId = GetVal(dict, "UnidadAdministrativaId")
                            };

                            Func<OficinaProductora, Task<bool>> existeAsync =
                                //async o => (await _databaseService.GetAllAsync<OficinaProductora>()).Any(x => x.Codigo == o.Codigo);
                                async o => await ExistsByIdAsync<OficinaProductora>(o.Id);

                            var res = await _excelService.ImportarDesdeExcel<OficinaProductora>(
                                filePath, headers, crear, existeAsync, sheetName);

                            var unidades = await _databaseService.GetAllAsync<UnidadAdministrativa>();
                            var insertados = 0;

                            for (int i = 0; i < res.Entidades.Count; i++)
                            {
                                var ent = res.Entidades[i];
                                var fila = res.Filas[i];

                                var unidadId = GetVal(fila, "UnidadAdministrativaId");
                                if (!unidades.Any(u => u.Id == unidadId))
                                {
                                    res.Errores.Add((i + 2, $"No se encontró UnidadAdministrativa para UnidadAdministrativaId='{unidadId}'"));
                                    continue;
                                }

                                try
                                {
                                    await _databaseService.InsertAsync(ent);
                                    insertados++;
                                }
                                catch (Exception ex)
                                {
                                    res.Errores.Add((i + 2, $"Insert error: {ex.Message}"));
                                }
                            }

                            await Application.Current.MainPage.DisplayAlert("Importación",
                                $"Detectados: {res.Importados}. Insertados: {insertados}. Errores: {res.Errores.Count}", "OK");
                            await LoadDataAsync();
                            break;
                        }

                    case "Series":
                        {
                            var oficinasExist = (await _databaseService.GetAllAsync<OficinaProductora>()).Any();
                            if (!oficinasExist)
                            {
                                await Application.Current.MainPage.DisplayAlert("Importación",
                                    "No existen Oficinas Productoras. Importe primero las Oficinas antes de las Series.", "OK");
                                return;
                            }

                            var headers = new[] { "Id", "Codigo", "Nombre", "Observacion", "OficinaProductoraId" };

                            Func<Dictionary<string, string>, Serie> crear = dict => new Serie
                            {
                                Id = GetIdOrNew(dict, "Id"),
                                Codigo = GetVal(dict, "Codigo"),
                                Nombre = GetVal(dict, "Nombre"),
                                Observacion = GetVal(dict, "Observacion"),
                                OficinaProductoraId = GetVal(dict, "OficinaProductoraId")
                            };

                            Func<Serie, Task<bool>> existeAsync =
                                //async s => (await _databaseService.GetAllAsync<Serie>()).Any(x => x.Codigo == s.Codigo);
                                async s => await ExistsByIdAsync<Serie>(s.Id);

                            var res = await _excelService.ImportarDesdeExcel<Serie>(
                                filePath, headers, crear, existeAsync, sheetName);

                            var oficinas = await _databaseService.GetAllAsync<OficinaProductora>();
                            var insertados = 0;

                            for (int i = 0; i < res.Entidades.Count; i++)
                            {
                                var ent = res.Entidades[i];
                                var fila = res.Filas[i];

                                var oficinaId = GetVal(fila, "OficinaProductoraId");
                                if (!oficinas.Any(o => o.Id == oficinaId))
                                {
                                    res.Errores.Add((i + 2, $"No se encontró OficinaProductora para OficinaProductoraId='{oficinaId}'"));
                                    continue;
                                }

                                try
                                {
                                    await _databaseService.InsertAsync(ent);
                                    insertados++;
                                }
                                catch (Exception ex)
                                {
                                    res.Errores.Add((i + 2, $"Insert error: {ex.Message}"));
                                }
                            }

                            await Application.Current.MainPage.DisplayAlert("Importación",
                                $"Detectados: {res.Importados}. Insertados: {insertados}. Errores: {res.Errores.Count}", "OK");
                            await LoadDataAsync();
                            break;
                        }

                    case "Subseries":
                        {
                            var seriesExist = (await _databaseService.GetAllAsync<Serie>()).Any();
                            if (!seriesExist)
                            {
                                await Application.Current.MainPage.DisplayAlert("Importación",
                                    "No existen Series. Importe primero las Series antes de las Subseries.", "OK");
                                return;
                            }

                            var headers = new[]
                            {
                    //"Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "P", "EL", "FormatoDigital", "CT", "E", "MT", "S", "Procedimiento"
                    "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "CT", "E", "MT", "S", "Procedimiento"
                };

                            Func<Dictionary<string, string>, Subserie> crear = dict => new Subserie
                            {
                                Id = GetIdOrNew(dict, "Id"),
                                Codigo = GetVal(dict, "Codigo"),
                                Nombre = GetVal(dict, "Nombre"),
                                Observacion = GetVal(dict, "Observacion"),
                                SerieId = GetVal(dict, "SerieId"),

                                AG = ParseInt(dict, "AG"),
                                AC = ParseInt(dict, "AC"),
                                //P = ParseBool(dict, "P"),
                                //EL = ParseBool(dict, "EL"),
                                //FormatoDigital = GetVal(dict, "FormatoDigital"),
                                CT = ParseBool(dict, "CT"),
                                E = ParseBool(dict, "E"),
                                MT = ParseBool(dict, "MT"),
                                S = ParseBool(dict, "S"),
                                Procedimiento = GetVal(dict, "Procedimiento")
                            };

                            Func<Subserie, Task<bool>> existeAsync =
                                //async ss => (await _databaseService.GetAllAsync<Subserie>()).Any(x => x.Codigo == ss.Codigo);
                                async ss => await ExistsByIdAsync<Subserie>(ss.Id);

                            var res = await _excelService.ImportarDesdeExcel<Subserie>(
                                filePath, headers, crear, existeAsync, sheetName);

                            var series = await _databaseService.GetAllAsync<Serie>();
                            var insertados = 0;

                            for (int i = 0; i < res.Entidades.Count; i++)
                            {
                                var ent = res.Entidades[i];
                                var fila = res.Filas[i];

                                var serieId = GetVal(fila, "SerieId");
                                if (!series.Any(s => s.Id == serieId))
                                {
                                    res.Errores.Add((i + 2, $"No se encontró Serie para SerieId='{serieId}'"));
                                    continue;
                                }

                                try
                                {
                                    await _databaseService.InsertAsync(ent);
                                    insertados++;
                                }
                                catch (Exception ex)
                                {
                                    res.Errores.Add((i + 2, $"Insert error: {ex.Message}"));
                                }
                            }

                            await Application.Current.MainPage.DisplayAlert("Importación",
                                $"Detectados: {res.Importados}. Insertados: {insertados}. Errores: {res.Errores.Count}", "OK");
                            await LoadDataAsync();
                            break;
                        }

                    case "TiposDocumentales":
                        {
                            var subseriesExist = (await _databaseService.GetAllAsync<Subserie>()).Any();
                            if (!subseriesExist)
                            {
                                await Application.Current.MainPage.DisplayAlert("Importación",
                                    "No existen Subseries. Importe primero las Subseries antes de los Tipos Documentales.", "OK");
                                return;
                            }

                            var headers = new[] { "Id", "Codigo", "Nombre", "Observacion", "SubserieId" };

                            Func<Dictionary<string, string>, TipoDocumental> crear = dict => new TipoDocumental
                            {
                                Id = GetIdOrNew(dict, "Id"),
                                Codigo = GetVal(dict, "Codigo"),
                                Nombre = GetVal(dict, "Nombre"),
                                P = ParseBool(dict, "P"),
                                EL = ParseBool(dict, "EL"),
                                FormatoDigital = GetVal(dict, "FormatoDigital"),
                                Observacion = GetVal(dict, "Observacion"),
                                SubserieId = GetVal(dict, "SubserieId")
                            };

                            Func<TipoDocumental, Task<bool>> existeAsync =
                                //async t => (await _databaseService.GetAllAsync<TipoDocumental>()).Any(x => x.Codigo == t.Codigo);
                                async t => await ExistsByIdAsync<TipoDocumental>(t.Id);

                            var res = await _excelService.ImportarDesdeExcel<TipoDocumental>(
                                filePath, headers, crear, existeAsync, sheetName);

                            var subseries = await _databaseService.GetAllAsync<Subserie>();
                            var insertados = 0;

                            for (int i = 0; i < res.Entidades.Count; i++)
                            {
                                var ent = res.Entidades[i];
                                var fila = res.Filas[i];

                                var subserieId = GetVal(fila, "SubserieId");
                                if (!subseries.Any(s => s.Id == subserieId))
                                {
                                    res.Errores.Add((i + 2, $"No se encontró Subserie para SubserieId='{subserieId}'"));
                                    continue;
                                }

                                try
                                {
                                    await _databaseService.InsertAsync(ent);
                                    insertados++;
                                }
                                catch (Exception ex)
                                {
                                    res.Errores.Add((i + 2, $"Insert error: {ex.Message}"));
                                }
                            }

                            await Application.Current.MainPage.DisplayAlert("Importación",
                                $"Detectados: {res.Importados}. Insertados: {insertados}. Errores: {res.Errores.Count}", "OK");
                            await LoadDataAsync();
                            break;
                        }

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
                        {
                            var fondos = await _databaseService.GetAllAsync<Fondo>();

                            var confirmCascade = await Application.Current.MainPage.DisplayAlert("Exportación",
                                "¿Exportar Fondos con todos sus subniveles en un único archivo (una hoja por tabla)?", "Sí", "No");

                            if (confirmCascade)
                            {
                                var subfondos = await _databaseService.GetAllAsync<Subfondo>();
                                var unidades = await _databaseService.GetAllAsync<UnidadAdministrativa>();
                                var oficinas = await _databaseService.GetAllAsync<OficinaProductora>();
                                var series = await _databaseService.GetAllAsync<Serie>();
                                var subseries = await _databaseService.GetAllAsync<Subserie>();
                                var tipos = await _databaseService.GetAllAsync<TipoDocumental>();

                                var sheets = new Dictionary<string, (IEnumerable<object>, string[], Type)>
                            {
                                { "Fondos", (fondos.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion" }, typeof(Fondo)) },
                                { "Subfondos", (subfondos.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "FondoId" }, typeof(Subfondo)) },
                                { "UnidadesAdministrativas", (unidades.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SubfondoId" }, typeof(UnidadAdministrativa)) },
                                { "OficinasProductoras", (oficinas.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "UnidadAdministrativaId" }, typeof(OficinaProductora)) },
                                { "Series", (series.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "OficinaProductoraId" }, typeof(Serie)) },
                                { "Subseries", (subseries.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "CT", "E", "MT", "S", "Procedimiento" }, typeof(Subserie)) },
                                { "TiposDocumentales", (tipos.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "P", "EL", "FormatoDigital", "Observacion", "SubserieId" }, typeof(TipoDocumental)) },
                            };

                                await _excelService.ExportarMultiplesAExcel(sheets, filePath);
                            }
                            else
                            {
                                await _excelService.ExportarAExcel(fondos.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion" });
                            }

                            break;
                        }

                    case "Subfondos":
                        {
                            var subfondos = await _databaseService.GetAllAsync<Subfondo>();

                            var confirmCascade = await Application.Current.MainPage.DisplayAlert("Exportación",
                                "¿Exportar Subfondos con sus descendientes (UnidadesAdministrativas, OficinasProductoras, Series, Subseries, TiposDocumentales)?", "Sí", "No");

                            if (confirmCascade)
                            {
                                var unidades = await _databaseService.GetAllAsync<UnidadAdministrativa>();
                                var oficinas = await _databaseService.GetAllAsync<OficinaProductora>();
                                var series = await _databaseService.GetAllAsync<Serie>();
                                var subseries = await _databaseService.GetAllAsync<Subserie>();
                                var tipos = await _databaseService.GetAllAsync<TipoDocumental>();

                                var sheets = new Dictionary<string, (IEnumerable<object>, string[], Type)>
                            {
                                { "Subfondos", (subfondos.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "FondoId" }, typeof(Subfondo)) },
                                { "UnidadesAdministrativas", (unidades.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SubfondoId" }, typeof(UnidadAdministrativa)) },
                                { "OficinasProductoras", (oficinas.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "UnidadAdministrativaId" }, typeof(OficinaProductora)) },
                                { "Series", (series.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "OficinaProductoraId" }, typeof(Serie)) },
                                { "Subseries", (subseries.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "P", "EL", "FormatoDigital", "CT", "E", "MT", "S", "Procedimiento" }, typeof(Subserie)) },
                                { "TiposDocumentales", (tipos.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SubserieId" }, typeof(TipoDocumental)) },
                            };

                                await _excelService.ExportarMultiplesAExcel(sheets, filePath);
                            }
                            else
                            {
                                await _excelService.ExportarAExcel(subfondos.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion", "FondoId" });
                            }

                            break;
                        }

                    case "UnidadesAdministrativas":
                        {
                            var unidades = await _databaseService.GetAllAsync<UnidadAdministrativa>();

                            var confirmCascade = await Application.Current.MainPage.DisplayAlert("Exportación",
                                "¿Exportar Unidades Administrativas con sus descendientes (OficinasProductoras, Series, Subseries, TiposDocumentales)?", "Sí", "No");

                            if (confirmCascade)
                            {
                                var oficinas = await _databaseService.GetAllAsync<OficinaProductora>();
                                var series = await _databaseService.GetAllAsync<Serie>();
                                var subseries = await _databaseService.GetAllAsync<Subserie>();
                                var tipos = await _databaseService.GetAllAsync<TipoDocumental>();

                                var sheets = new Dictionary<string, (IEnumerable<object>, string[], Type)>
                            {
                                { "UnidadesAdministrativas", (unidades.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SubfondoId" }, typeof(UnidadAdministrativa)) },
                                { "OficinasProductoras", (oficinas.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "UnidadAdministrativaId" }, typeof(OficinaProductora)) },
                                { "Series", (series.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "OficinaProductoraId" }, typeof(Serie)) },
                                { "Subseries", (subseries.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "P", "EL", "FormatoDigital", "CT", "E", "MT", "S", "Procedimiento" }, typeof(Subserie)) },
                                { "TiposDocumentales", (tipos.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SubserieId" }, typeof(TipoDocumental)) },
                            };

                                await _excelService.ExportarMultiplesAExcel(sheets, filePath);
                            }
                            else
                            {
                                await _excelService.ExportarAExcel(unidades.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion", "SubfondoId" });
                            }

                            break;
                        }

                    case "OficinasProductoras":
                        {
                            var oficinas = await _databaseService.GetAllAsync<OficinaProductora>();

                            var confirmCascade = await Application.Current.MainPage.DisplayAlert("Exportación",
                                "¿Exportar Oficinas Productoras con sus descendientes (Series, Subseries, TiposDocumentales)?", "Sí", "No");

                            if (confirmCascade)
                            {
                                var series = await _databaseService.GetAllAsync<Serie>();
                                var subseries = await _databaseService.GetAllAsync<Subserie>();
                                var tipos = await _databaseService.GetAllAsync<TipoDocumental>();

                                var sheets = new Dictionary<string, (IEnumerable<object>, string[], Type)>
                            {
                                { "OficinasProductoras", (oficinas.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "UnidadAdministrativaId" }, typeof(OficinaProductora)) },
                                { "Series", (series.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "OficinaProductoraId" }, typeof(Serie)) },
                                { "Subseries", (subseries.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "P", "EL", "FormatoDigital", "CT", "E", "MT", "S", "Procedimiento" }, typeof(Subserie)) },
                                { "TiposDocumentales", (tipos.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SubserieId" }, typeof(TipoDocumental)) },
                            };

                                await _excelService.ExportarMultiplesAExcel(sheets, filePath);
                            }
                            else
                            {
                                await _excelService.ExportarAExcel(oficinas.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion", "UnidadAdministrativaId" });
                            }

                            break;
                        }

                    case "Series":
                        {
                            var series = await _databaseService.GetAllAsync<Serie>();

                            var confirmCascade = await Application.Current.MainPage.DisplayAlert("Exportación",
                                "¿Exportar Series con sus descendientes (Subseries, Tipos Documentales)?", "Sí", "No");

                            if (confirmCascade)
                            {
                                var subseries = await _databaseService.GetAllAsync<Subserie>();
                                var tipos = await _databaseService.GetAllAsync<TipoDocumental>();

                                var sheets = new Dictionary<string, (IEnumerable<object>, string[], Type)>
                            {
                                { "Series", (series.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "OficinaProductoraId" }, typeof(Serie)) },
                                { "Subseries", (subseries.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "P", "EL", "FormatoDigital", "CT", "E", "MT", "S", "Procedimiento" }, typeof(Subserie)) },
                                { "TiposDocumentales", (tipos.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SubserieId" }, typeof(TipoDocumental)) },
                            };

                                await _excelService.ExportarMultiplesAExcel(sheets, filePath);
                            }
                            else
                            {
                                await _excelService.ExportarAExcel(series.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion", "OficinaProductoraId" });
                            }

                            break;
                        }

                    case "Subseries":
                        {
                            var subseries = await _databaseService.GetAllAsync<Subserie>();

                            var confirmCascade = await Application.Current.MainPage.DisplayAlert("Exportación",
                                "¿Exportar Subseries con sus Tipos Documentales?", "Sí", "No");

                            if (confirmCascade)
                            {
                                var tipos = await _databaseService.GetAllAsync<TipoDocumental>();

                                var sheets = new Dictionary<string, (IEnumerable<object>, string[], Type)>
                            {
                                //{ "Subseries", (subseries.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "P", "EL", "FormatoDigital", "CT", "E", "MT", "S", "Procedimiento" }, typeof(Subserie)) },
                                //{ "TiposDocumentales", (tipos.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SubserieId" }, typeof(TipoDocumental)) },
                                { "Subseries", (subseries.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "CT", "E", "MT", "S", "Procedimiento" }, typeof(Subserie)) },
                                { "TiposDocumentales", (tipos.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "P", "EL", "FormatoDigital", "Observacion", "SubserieId" }, typeof(TipoDocumental)) },

                            };

                                await _excelService.ExportarMultiplesAExcel(sheets, filePath);
                            }
                            else
                            {
                                //await _excelService.ExportarAExcel(subseries.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "P", "EL", "FormatoDigital", "CT", "E", "MT", "S", "Procedimiento" });
                                await _excelService.ExportarAExcel(subseries.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "CT", "E", "MT", "S", "Procedimiento" });
                            }

                            break;
                        }

                    case "TiposDocumentales":
                        {
                            var tipos = await _databaseService.GetAllAsync<TipoDocumental>();
                            // Tipos es el último nivel; solo exportar hoja simple
                            //await _excelService.ExportarAExcel(tipos.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion", "SubserieId" });
                            await _excelService.ExportarAExcel(tipos.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "P", "EL", "FormatoDigital", "Observacion", "SubserieId" });
                            break;
                        }

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