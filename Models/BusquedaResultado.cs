namespace DocumentalManager.Models
{
    public class BusquedaResultado
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
    }
}