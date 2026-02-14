using SQLite;
using System;

namespace DocumentalManager.Models
{
    public abstract class BaseEntity
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
    }
}