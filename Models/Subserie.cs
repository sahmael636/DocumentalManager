
namespace DocumentalManager.Models
{
    public class Subserie : BaseEntity
    {
        public int SerieId { get; set; }
        public int AG { get; set; } // Archivo de Gestión
        public int AC { get; set; } // Archivo Central
        public bool P { get; set; } // Papel
        public bool EL { get; set; } // Electrónico
        public string FormatoDigital { get; set; } = string.Empty;
        public bool CT { get; set; } // Conservación Total
        public bool E { get; set; } // Eliminación
        public bool MT { get; set; } // Medios Tecnológico
        public bool S { get; set; } // Selección
        public string Procedimiento { get; set; } = string.Empty;
    }
}