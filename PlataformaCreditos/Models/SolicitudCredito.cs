using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlataformaCreditos.Models
{
    public class SolicitudCredito
    {
        public int Id { get; set; }
        
        [Required]
        public int ClienteId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoSolicitado { get; set; }
        
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        
        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;
        
        [MaxLength(500)]
        public string? MotivoRechazo { get; set; }
        
        // Navegación
        public virtual Cliente? Cliente { get; set; }
    }
}