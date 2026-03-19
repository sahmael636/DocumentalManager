using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Windows.Input;

namespace DocumentalManager.Models
{
    public partial class BusquedaResultado : ObservableObject
    {
        public string Fondo { get; set; } = string.Empty;
        public string Subfondo { get; set; } = string.Empty;
        public string UnidadAdministrativa { get; set; } = string.Empty;
        public string OficinaProductora { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public string Subserie { get; set; } = string.Empty;
        public string TipoDocumental { get; set; } = string.Empty;
        public string CodigoCompleto { get; set; } = string.Empty;

        // Propiedades de Subserie
        public int AG { get; set; }
        public int AC { get; set; }
        public bool Papel { get; set; }
        public bool Electronico { get; set; }
        public string FormatoDigital { get; set; } = string.Empty;
        public bool ConservacionTotal { get; set; }
        public bool Eliminacion { get; set; }
        public bool MediosTecnologicos { get; set; }
        public bool Seleccion { get; set; }
        public string Procedimiento { get; set; } = string.Empty;

        // Propiedad calculada para mostrar los soportes (Papel / Electrónico)
        public string Soporte
        {
            get
            {
                var parts = new List<string>();
                if (Papel) parts.Add("Papel");
                if (Electronico) parts.Add("Electrónico");
                return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
            }
        }

        // Propiedad calculada para mostrar la disposición abreviada (CT, E, MT, S)
        public string Disposicion
        {
            get
            {
                var parts = new List<string>();
                if (ConservacionTotal) parts.Add("Cons Total");
                if (Eliminacion) parts.Add("Elimin");
                if (MediosTecnologicos) parts.Add("Med Tec");
                if (Seleccion) parts.Add("Selec");
                return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
            }
        }

        // Estado de despliegue del procedimiento (no se persiste en DB)
        [ObservableProperty]
        private bool procedimientoExpanded;

        // Controla cuántas líneas mostrar (1 por defecto, ilimitado cuando está expandido)
        public int ProcedimientoMaxLines => ProcedimientoExpanded ? int.MaxValue : 1;

        // Texto para el botón/label de alternancia
        public string ProcedimientoToggleText => ProcedimientoExpanded ? "Mostrar menos" : "Mostrar más";

        // Comando para alternar el estado (se enlazará desde la plantilla)
        private RelayCommand? _toggleProcedimientoCommand;
        public ICommand ToggleProcedimientoCommand => _toggleProcedimientoCommand ??= new RelayCommand(() =>
        {
            ProcedimientoExpanded = !ProcedimientoExpanded;
        });

        // Cuando cambia el booleano, notificar que ProcedimientoMaxLines y el texto cambiaron
        partial void OnProcedimientoExpandedChanged(bool value)
        {
            OnPropertyChanged(nameof(ProcedimientoMaxLines));
            OnPropertyChanged(nameof(ProcedimientoToggleText));
        }
    }
}