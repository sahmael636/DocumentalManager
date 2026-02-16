namespace DocumentalManager.Models
{
    public class TipoDocumental : BaseEntity
    {
        public string SubserieId { get; set; } = string.Empty;
        public bool P { get; set; } // Papel
        public bool EL { get; set; } // Electrónico
        public string FormatoDigital { get; set; } = string.Empty;
    }
}