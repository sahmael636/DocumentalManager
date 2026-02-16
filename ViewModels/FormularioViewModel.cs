using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentalManager.Models;
using DocumentalManager.Services;
using System.Collections.ObjectModel;

namespace DocumentalManager.ViewModels
{
    public partial class FormularioViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private string tableName;

        [ObservableProperty]
        private string id; // Cambiado de int a string

        [ObservableProperty]
        private string codigo;

        [ObservableProperty]
        private string nombre;

        [ObservableProperty]
        private string observacion;

        [ObservableProperty]
        private ObservableCollection<object> itemsPadre;

        [ObservableProperty]
        private object selectedItemPadre;

        [ObservableProperty]
        private int ag;

        [ObservableProperty]
        private int ac;

        [ObservableProperty]
        private bool p;

        [ObservableProperty]
        private bool el;

        [ObservableProperty]
        private string formatoDigital;

        [ObservableProperty]
        private bool ct;

        [ObservableProperty]
        private bool e;

        [ObservableProperty]
        private bool mt;

        [ObservableProperty]
        private bool s;

        [ObservableProperty]
        private string procedimiento;

        [ObservableProperty]
        private ObservableCollection<string> formatosDisponibles = new ObservableCollection<string>
        {
            "PDF", "XLS", "JPG", "PNG", "DOC", "MP4", "MP3", "TXT"
        };

        public FormularioViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            ItemsPadre = new ObservableCollection<object>();
        }

        public async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                await LoadParentItems();

                if (!string.IsNullOrEmpty(Id) && Id != "new") // Cambiado de Id > 0
                {
                    await LoadExistingData();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadParentItems()
        {
            switch (TableName)
            {
                case "Subfondos":
                    var fondos = await _databaseService.GetAllAsync<Fondo>();
                    foreach (var fondo in fondos)
                        ItemsPadre.Add(fondo);
                    break;
                case "UnidadesAdministrativas":
                    var subfondos = await _databaseService.GetAllAsync<Subfondo>();
                    foreach (var subfondo in subfondos)
                        ItemsPadre.Add(subfondo);
                    break;
                case "OficinasProductoras":
                    var unidades = await _databaseService.GetAllAsync<UnidadAdministrativa>();
                    foreach (var unidad in unidades)
                        ItemsPadre.Add(unidad);
                    break;
                case "Series":
                    var oficinas = await _databaseService.GetAllAsync<OficinaProductora>();
                    foreach (var oficina in oficinas)
                        ItemsPadre.Add(oficina);
                    break;
                case "Subseries":
                    var series = await _databaseService.GetAllAsync<Serie>();
                    foreach (var serie in series)
                        ItemsPadre.Add(serie);
                    break;
                case "TiposDocumentales":
                    var subseries = await _databaseService.GetAllAsync<Subserie>();
                    foreach (var subserie in subseries)
                        ItemsPadre.Add(subserie);
                    break;
            }
        }

        private async Task LoadExistingData()
        {
            switch (TableName)
            {
                case "Fondos":
                    var fondo = await _databaseService.GetByIdAsync<Fondo>(Id);
                    if (fondo != null)
                    {
                        Codigo = fondo.Codigo;
                        Nombre = fondo.Nombre;
                        Observacion = fondo.Observacion;
                    }
                    break;
                case "Subfondos":
                    var subfondo = await _databaseService.GetByIdAsync<Subfondo>(Id);
                    if (subfondo != null)
                    {
                        Codigo = subfondo.Codigo;
                        Nombre = subfondo.Nombre;
                        Observacion = subfondo.Observacion;
                        SelectedItemPadre = ItemsPadre.FirstOrDefault(i => ((Fondo)i).Id == subfondo.FondoId);
                    }
                    break;
                case "UnidadesAdministrativas":
                    var unidad = await _databaseService.GetByIdAsync<UnidadAdministrativa>(Id);
                    if (unidad != null)
                    {
                        Codigo = unidad.Codigo;
                        Nombre = unidad.Nombre;
                        Observacion = unidad.Observacion;
                        SelectedItemPadre = ItemsPadre.FirstOrDefault(i => ((Subfondo)i).Id == unidad.SubfondoId);
                    }
                    break;
                case "OficinasProductoras":
                    var oficina = await _databaseService.GetByIdAsync<OficinaProductora>(Id);
                    if (oficina != null)
                    {
                        Codigo = oficina.Codigo;
                        Nombre = oficina.Nombre;
                        Observacion = oficina.Observacion;
                        SelectedItemPadre = ItemsPadre.FirstOrDefault(i => ((UnidadAdministrativa)i).Id == oficina.UnidadAdministrativaId);
                    }
                    break;
                case "Series":
                    var serie = await _databaseService.GetByIdAsync<Serie>(Id);
                    if (serie != null)
                    {
                        Codigo = serie.Codigo;
                        Nombre = serie.Nombre;
                        Observacion = serie.Observacion;
                        SelectedItemPadre = ItemsPadre.FirstOrDefault(i => ((OficinaProductora)i).Id == serie.OficinaProductoraId);
                    }
                    break;
                case "Subseries":
                    var subserie = await _databaseService.GetByIdAsync<Subserie>(Id);
                    if (subserie != null)
                    {
                        Codigo = subserie.Codigo;
                        Nombre = subserie.Nombre;
                        Ag = subserie.AG;
                        Ac = subserie.AC;
                        P = subserie.P;
                        El = subserie.EL;
                        FormatoDigital = subserie.FormatoDigital;
                        Ct = subserie.CT;
                        E = subserie.E;
                        Mt = subserie.MT;
                        S = subserie.S;
                        Procedimiento = subserie.Procedimiento;
                        SelectedItemPadre = ItemsPadre.FirstOrDefault(i => ((Serie)i).Id == subserie.SerieId);
                    }
                    break;
                case "TiposDocumentales":
                    var tipo = await _databaseService.GetByIdAsync<TipoDocumental>(Id);
                    if (tipo != null)
                    {
                        Codigo = tipo.Codigo;
                        Nombre = tipo.Nombre;
                        Observacion = tipo.Observacion;
                        SelectedItemPadre = ItemsPadre.FirstOrDefault(i => ((Subserie)i).Id == tipo.SubserieId);
                    }
                    break;
            }
        }

        [RelayCommand]
        private async Task Guardar()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Codigo) || string.IsNullOrWhiteSpace(Nombre))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Código y Nombre son requeridos", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(Id) || Id == "new") // Cambiado de Id == 0
                    await InsertarNuevo();
                else
                    await ActualizarExistente();

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async Task InsertarNuevo()
        {
            switch (TableName)
            {
                case "Fondos":
                    await _databaseService.InsertAsync(new Fondo
                    {
                        Codigo = Codigo,
                        Nombre = Nombre,
                        Observacion = Observacion
                    });
                    break;
                case "Subfondos":
                    await _databaseService.InsertAsync(new Subfondo
                    {
                        Codigo = Codigo,
                        Nombre = Nombre,
                        Observacion = Observacion,
                        FondoId = SelectedItemPadre != null ? ((Fondo)SelectedItemPadre).Id : string.Empty // Cambiado de 0 a string.Empty
                    });
                    break;
                case "UnidadesAdministrativas":
                    await _databaseService.InsertAsync(new UnidadAdministrativa
                    {
                        Codigo = Codigo,
                        Nombre = Nombre,
                        Observacion = Observacion,
                        SubfondoId = SelectedItemPadre != null ? ((Subfondo)SelectedItemPadre).Id : string.Empty // Cambiado de 0 a string.Empty
                    });
                    break;
                case "OficinasProductoras":
                    await _databaseService.InsertAsync(new OficinaProductora
                    {
                        Codigo = Codigo,
                        Nombre = Nombre,
                        Observacion = Observacion,
                        UnidadAdministrativaId = SelectedItemPadre != null ? ((UnidadAdministrativa)SelectedItemPadre).Id : string.Empty // Cambiado de 0 a string.Empty
                    });
                    break;
                case "Series":
                    await _databaseService.InsertAsync(new Serie
                    {
                        Codigo = Codigo,
                        Nombre = Nombre,
                        Observacion = Observacion,
                        OficinaProductoraId = SelectedItemPadre != null ? ((OficinaProductora)SelectedItemPadre).Id : string.Empty // Cambiado de 0 a string.Empty
                    });
                    break;
                case "Subseries":
                    await _databaseService.InsertAsync(new Subserie
                    {
                        Codigo = Codigo,
                        Nombre = Nombre,
                        SerieId = SelectedItemPadre != null ? ((Serie)SelectedItemPadre).Id : string.Empty, // Cambiado de 0 a string.Empty
                        AG = ag,
                        AC = ac,
                        P = P,
                        EL = el,
                        FormatoDigital = FormatoDigital,
                        CT = ct,
                        E = E,
                        MT = mt,
                        S = S,
                        Procedimiento = Procedimiento
                    });
                    break;
                case "TiposDocumentales":
                    await _databaseService.InsertAsync(new TipoDocumental
                    {
                        Codigo = Codigo,
                        Nombre = Nombre,
                        Observacion = Observacion,
                        SubserieId = SelectedItemPadre != null ? ((Subserie)SelectedItemPadre).Id : string.Empty // Cambiado de 0 a string.Empty
                    });
                    break;
            }
        }

        private async Task ActualizarExistente()
        {
            switch (TableName)
            {
                case "Fondos":
                    var fondo = await _databaseService.GetByIdAsync<Fondo>(Id);
                    if (fondo != null)
                    {
                        fondo.Codigo = Codigo;
                        fondo.Nombre = Nombre;
                        fondo.Observacion = Observacion;
                        await _databaseService.UpdateAsync(fondo);
                    }
                    break;
                case "Subfondos":
                    var subfondo = await _databaseService.GetByIdAsync<Subfondo>(Id);
                    if (subfondo != null)
                    {
                        subfondo.Codigo = Codigo;
                        subfondo.Nombre = Nombre;
                        subfondo.Observacion = Observacion;
                        subfondo.FondoId = SelectedItemPadre != null ? ((Fondo)SelectedItemPadre).Id : string.Empty; // Cambiado de 0 a string.Empty
                        await _databaseService.UpdateAsync(subfondo);
                    }
                    break;
                case "UnidadesAdministrativas":
                    var unidad = await _databaseService.GetByIdAsync<UnidadAdministrativa>(Id);
                    if (unidad != null)
                    {
                        unidad.Codigo = Codigo;
                        unidad.Nombre = Nombre;
                        unidad.Observacion = Observacion;
                        unidad.SubfondoId = SelectedItemPadre != null ? ((Subfondo)SelectedItemPadre).Id : string.Empty; // Cambiado de 0 a string.Empty
                        await _databaseService.UpdateAsync(unidad);
                    }
                    break;
                case "OficinasProductoras":
                    var oficina = await _databaseService.GetByIdAsync<OficinaProductora>(Id);
                    if (oficina != null)
                    {
                        oficina.Codigo = Codigo;
                        oficina.Nombre = Nombre;
                        oficina.Observacion = Observacion;
                        oficina.UnidadAdministrativaId = SelectedItemPadre != null ? ((UnidadAdministrativa)SelectedItemPadre).Id : string.Empty; // Cambiado de 0 a string.Empty
                        await _databaseService.UpdateAsync(oficina);
                    }
                    break;
                case "Series":
                    var serie = await _databaseService.GetByIdAsync<Serie>(Id);
                    if (serie != null)
                    {
                        serie.Codigo = Codigo;
                        serie.Nombre = Nombre;
                        serie.Observacion = Observacion;
                        serie.OficinaProductoraId = SelectedItemPadre != null ? ((OficinaProductora)SelectedItemPadre).Id : string.Empty; // Cambiado de 0 a string.Empty
                        await _databaseService.UpdateAsync(serie);
                    }
                    break;
                case "Subseries":
                    var subserie = await _databaseService.GetByIdAsync<Subserie>(Id);
                    if (subserie != null)
                    {
                        subserie.Codigo = Codigo;
                        subserie.Nombre = Nombre;
                        subserie.SerieId = SelectedItemPadre != null ? ((Serie)SelectedItemPadre).Id : string.Empty; // Cambiado de 0 a string.Empty
                        subserie.AG = ag;
                        subserie.AC = ac;
                        subserie.P = P;
                        subserie.EL = el;
                        subserie.FormatoDigital = FormatoDigital;
                        subserie.CT = ct;
                        subserie.E = E;
                        subserie.MT = mt;
                        subserie.S = S;
                        subserie.Procedimiento = Procedimiento;
                        await _databaseService.UpdateAsync(subserie);
                    }
                    break;
                case "TiposDocumentales":
                    var tipo = await _databaseService.GetByIdAsync<TipoDocumental>(Id);
                    if (tipo != null)
                    {
                        tipo.Codigo = Codigo;
                        tipo.Nombre = Nombre;
                        tipo.Observacion = Observacion;
                        tipo.SubserieId = SelectedItemPadre != null ? ((Subserie)SelectedItemPadre).Id : string.Empty; // Cambiado de 0 a string.Empty
                        await _databaseService.UpdateAsync(tipo);
                    }
                    break;
            }
        }
    }
}