namespace DocumentalManager.Models
{
    public class TipoDocumental
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int SubserieId { get; set; }
        public string Observacion { get; set; } = string.Empty;
    }
}