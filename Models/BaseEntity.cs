namespace DocumentalManager.Models
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
    }
}