using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PlataformaCreditos.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        
        [Required]
        public string UsuarioId { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal IngresosMensuales { get; set; }
        
        public bool Activo { get; set; } = true;
        
        // Navegación
        public virtual IdentityUser? Usuario { get; set; }
        public virtual ICollection<SolicitudCredito>? Solicitudes { get; set; }
    }
}