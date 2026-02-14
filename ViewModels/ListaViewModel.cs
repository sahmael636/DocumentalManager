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
                            }
                        }

                        await Application.Current.MainPage.DisplayAlert("Importación",
                            $"Detectados: {res.Importados}. Insertados: {insertados}. Errores: {res.Errores.Count}", "OK");
                        await LoadDataAsync();
                        break;
                    }

                    case "Subfondos":
                    {
                        // Validar que existan Fondos primero
                        var fondosExist = (await _databaseService.GetAllAsync<Fondo>()).Any();
                        if (!fondosExist)
                        {
                            await Application.Current.MainPage.DisplayAlert("Importación",
                                "No existen Fondos en la base de datos. Importe primero los Fondos antes de importar Subfondos.", "OK");
                            return;
                        }

                        var headers = new[] { "Codigo", "Nombre", "Observacion", "FondoCodigo" };
                        Func<Dictionary<string, string>, Subfondo> crear = dict => new Subfondo
                        {
                            Codigo = dict.GetValueOrDefault("Codigo")?.Trim(),
                            Nombre = dict.GetValueOrDefault("Nombre")?.Trim(),
                            Observacion = dict.GetValueOrDefault("Observacion")?.Trim(),
                            FondoId = 0
                        };
                        Func<Subfondo, Task<bool>> existeAsync = async s =>
                            (await _databaseService.GetAllAsync<Subfondo>()).Any(x => x.Codigo == s.Codigo);

                        var res = await _excelService.ImportarDesdeExcel<Subfondo>(filePath, headers, crear, existeAsync);

                        var fondos = await _databaseService.GetAllAsync<Fondo>();
                        var insertados = 0;
                        for (int i = 0; i < res.Entidades.Count; i++)
                        {
                            var ent = res.Entidades[i];
                            var fila = res.Filas[i];
                            var fondoCodigo = fila.GetValueOrDefault("FondoCodigo")?.Trim();
                            var parent = fondos.FirstOrDefault(f => string.Equals(f.Codigo, fondoCodigo, StringComparison.OrdinalIgnoreCase));
                            if (parent == null)
                            {
                                res.Errores.Add((i + 2, $"No se encontró Fondo para FondoCodigo='{fondoCodigo}'"));
                                continue;
                            }

                            ent.FondoId = parent.Id;

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

                    case "UnidadesAdministrativas":
                    {
                        var subfondosExist = (await _databaseService.GetAllAsync<Subfondo>()).Any();
                        if (!subfondosExist)
                        {
                            await Application.Current.MainPage.DisplayAlert("Importación",
                                "No existen Subfondos en la base de datos. Importe primero los Subfondos antes de importar Unidades Administrativas.", "OK");
                            return;
                        }

                        var headers = new[] { "Codigo", "Nombre", "Observacion", "SubfondoCodigo" };
                        Func<Dictionary<string, string>, UnidadAdministrativa> crear = dict => new UnidadAdministrativa
                        {
                            Codigo = dict.GetValueOrDefault("Codigo")?.Trim(),
                            Nombre = dict.GetValueOrDefault("Nombre")?.Trim(),
                            Observacion = dict.GetValueOrDefault("Observacion")?.Trim(),
                            SubfondoId = 0
                        };
                        Func<UnidadAdministrativa, Task<bool>> existeAsync = async s =>
                            (await _databaseService.GetAllAsync<UnidadAdministrativa>()).Any(x => x.Codigo == s.Codigo);

                        var res = await _excelService.ImportarDesdeExcel<UnidadAdministrativa>(filePath, headers, crear, existeAsync);

                        var subfondos = await _databaseService.GetAllAsync<Subfondo>();
                        var insertados = 0;
                        for (int i = 0; i < res.Entidades.Count; i++)
                        {
                            var ent = res.Entidades[i];
                            var fila = res.Filas[i];
                            var parentCodigo = fila.GetValueOrDefault("SubfondoCodigo")?.Trim();
                            var parent = subfondos.FirstOrDefault(s => string.Equals(s.Codigo, parentCodigo, StringComparison.OrdinalIgnoreCase));
                            if (parent == null)
                            {
                                res.Errores.Add((i + 2, $"No se encontró Subfondo para SubfondoCodigo='{parentCodigo}'"));
                                continue;
                            }

                            ent.SubfondoId = parent.Id;

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

                        var headers = new[] { "Codigo", "Nombre", "Observacion", "UnidadCodigo" };
                        Func<Dictionary<string, string>, OficinaProductora> crear = dict => new OficinaProductora
                        {
                            Codigo = dict.GetValueOrDefault("Codigo")?.Trim(),
                            Nombre = dict.GetValueOrDefault("Nombre")?.Trim(),
                            Observacion = dict.GetValueOrDefault("Observacion")?.Trim(),
                            UnidadAdministrativaId = 0
                        };
                        Func<OficinaProductora, Task<bool>> existeAsync = async s =>
                            (await _databaseService.GetAllAsync<OficinaProductora>()).Any(x => x.Codigo == s.Codigo);

                        var res = await _excelService.ImportarDesdeExcel<OficinaProductora>(filePath, headers, crear, existeAsync);

                        var unidades = await _databaseService.GetAllAsync<UnidadAdministrativa>();
                        var insertados = 0;
                        for (int i = 0; i < res.Entidades.Count; i++)
                        {
                            var ent = res.Entidades[i];
                            var fila = res.Filas[i];
                            var parentCodigo = fila.GetValueOrDefault("UnidadCodigo")?.Trim();
                            var parent = unidades.FirstOrDefault(u => string.Equals(u.Codigo, parentCodigo, StringComparison.OrdinalIgnoreCase));
                            if (parent == null)
                            {
                                res.Errores.Add((i + 2, $"No se encontró Unidad para UnidadCodigo='{parentCodigo}'"));
                                continue;
                            }

                            ent.UnidadAdministrativaId = parent.Id;

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

                        var headers = new[] { "Codigo", "Nombre", "Observacion", "OficinaCodigo" };
                        Func<Dictionary<string, string>, Serie> crear = dict => new Serie
                        {
                            Codigo = dict.GetValueOrDefault("Codigo")?.Trim(),
                            Nombre = dict.GetValueOrDefault("Nombre")?.Trim(),
                            Observacion = dict.GetValueOrDefault("Observacion")?.Trim(),
                            OficinaProductoraId = 0
                        };
                        Func<Serie, Task<bool>> existeAsync = async s =>
                            (await _databaseService.GetAllAsync<Serie>()).Any(x => x.Codigo == s.Codigo);

                        var res = await _excelService.ImportarDesdeExcel<Serie>(filePath, headers, crear, existeAsync);

                        var oficinas = await _databaseService.GetAllAsync<OficinaProductora>();
                        var insertados = 0;
                        for (int i = 0; i < res.Entidades.Count; i++)
                        {
                            var ent = res.Entidades[i];
                            var fila = res.Filas[i];
                            var parentCodigo = fila.GetValueOrDefault("OficinaCodigo")?.Trim();
                            var parent = oficinas.FirstOrDefault(o => string.Equals(o.Codigo, parentCodigo, StringComparison.OrdinalIgnoreCase));
                            if (parent == null)
                            {
                                res.Errores.Add((i + 2, $"No se encontró Oficina para OficinaCodigo='{parentCodigo}'"));
                                continue;
                            }

                            ent.OficinaProductoraId = parent.Id;

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

                        var headers = new[] { "Codigo", "Nombre", "Observacion", "SerieCodigo", "AG","AC","P","EL","FormatoDigital","CT","E","MT","S","Procedimiento" };
                        Func<Dictionary<string, string>, Subserie> crear = dict => new Subserie
                        {
                            Codigo = dict.GetValueOrDefault("Codigo")?.Trim(),
                            Nombre = dict.GetValueOrDefault("Nombre")?.Trim(),
                            Observacion = dict.GetValueOrDefault("Observacion")?.Trim(),
                            SerieId = 0,
                            AG = int.TryParse(dict.GetValueOrDefault("AG"), out var ag) ? ag : 0,
                            AC = int.TryParse(dict.GetValueOrDefault("AC"), out var ac) ? ac : 0,
                            P = bool.TryParse(dict.GetValueOrDefault("P"), out var p) ? p : false,
                            EL = bool.TryParse(dict.GetValueOrDefault("EL"), out var el) ? el : false,
                            FormatoDigital = dict.GetValueOrDefault("FormatoDigital")?.Trim(),
                            CT = bool.TryParse(dict.GetValueOrDefault("CT"), out var ct) ? ct : false,
                            E = bool.TryParse(dict.GetValueOrDefault("E"), out var e) ? e : false,
                            MT = bool.TryParse(dict.GetValueOrDefault("MT"), out var mt) ? mt : false,
                            S = bool.TryParse(dict.GetValueOrDefault("S"), out var s) ? s : false,
                            Procedimiento = dict.GetValueOrDefault("Procedimiento")?.Trim(),
                        };
                        Func<Subserie, Task<bool>> existeAsync = async s =>
                            (await _databaseService.GetAllAsync<Subserie>()).Any(x => x.Codigo == s.Codigo);

                        var res = await _excelService.ImportarDesdeExcel<Subserie>(filePath, headers, crear, existeAsync);

                        var series = await _databaseService.GetAllAsync<Serie>();
                        var insertados = 0;
                        for (int i = 0; i < res.Entidades.Count; i++)
                        {
                            var ent = res.Entidades[i];
                            var fila = res.Filas[i];
                            var parentCodigo = fila.GetValueOrDefault("SerieCodigo")?.Trim();
                            var parent = series.FirstOrDefault(s => string.Equals(s.Codigo, parentCodigo, StringComparison.OrdinalIgnoreCase));
                            if (parent == null)
                            {
                                res.Errores.Add((i + 2, $"No se encontró Serie para SerieCodigo='{parentCodigo}'"));
                                continue;
                            }

                            ent.SerieId = parent.Id;

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

                        var headers = new[] { "Codigo", "Nombre", "Observacion", "SubserieCodigo" };
                        Func<Dictionary<string, string>, TipoDocumental> crear = dict => new TipoDocumental
                        {
                            Codigo = dict.GetValueOrDefault("Codigo")?.Trim(),
                            Nombre = dict.GetValueOrDefault("Nombre")?.Trim(),
                            Observacion = dict.GetValueOrDefault("Observacion")?.Trim(),
                            SubserieId = 0
                        };
                        Func<TipoDocumental, Task<bool>> existeAsync = async s =>
                            (await _databaseService.GetAllAsync<TipoDocumental>()).Any(x => x.Codigo == s.Codigo);

                        var res = await _excelService.ImportarDesdeExcel<TipoDocumental>(filePath, headers, crear, existeAsync);

                        var subseries = await _databaseService.GetAllAsync<Subserie>();
                        var insertados = 0;
                        for (int i = 0; i < res.Entidades.Count; i++)
                        {
                            var ent = res.Entidades[i];
                            var fila = res.Filas[i];
                            var parentCodigo = fila.GetValueOrDefault("SubserieCodigo")?.Trim();
                            var parent = subseries.FirstOrDefault(s => string.Equals(s.Codigo, parentCodigo, StringComparison.OrdinalIgnoreCase));
                            if (parent == null)
                            {
                                res.Errores.Add((i + 2, $"No se encontró Subserie para SubserieCodigo='{parentCodigo}'"));
                                continue;
                            }

                            ent.SubserieId = parent.Id;

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
                                { "Subseries", (subseries.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "P", "EL", "FormatoDigital", "CT", "E", "MT", "S", "Procedimiento" }, typeof(Subserie)) },
                                { "TiposDocumentales", (tipos.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SubserieId" }, typeof(TipoDocumental)) },
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
                            "¿Exportar Series con sus descendientes (Subseries, TiposDocumentales)?", "Sí", "No");

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
                                { "Subseries", (subseries.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "P", "EL", "FormatoDigital", "CT", "E", "MT", "S", "Procedimiento" }, typeof(Subserie)) },
                                { "TiposDocumentales", (tipos.Cast<object>(), new[] { "Id", "Codigo", "Nombre", "Observacion", "SubserieId" }, typeof(TipoDocumental)) },
                            };

                            await _excelService.ExportarMultiplesAExcel(sheets, filePath);
                        }
                        else
                        {
                            await _excelService.ExportarAExcel(subseries.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion", "SerieId", "AG", "AC", "P", "EL", "FormatoDigital", "CT", "E", "MT", "S", "Procedimiento" });
                        }

                        break;
                    }

                    case "TiposDocumentales":
                    {
                        var tipos = await _databaseService.GetAllAsync<TipoDocumental>();
                        // Tipos es el último nivel; solo exportar hoja simple
                        await _excelService.ExportarAExcel(tipos.ToList(), filePath, new[] { "Id", "Codigo", "Nombre", "Observacion", "SubserieId" });
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