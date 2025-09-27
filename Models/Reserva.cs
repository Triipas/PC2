using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PC2.Models
{
    public class Reserva
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El inmueble es obligatorio")]
        public int InmuebleId { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio")]
        public string UsuarioId { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de expiración es obligatoria")]
        [DataType(DataType.DateTime)]
        public DateTime FechaExpiracion { get; set; }

        [Required(ErrorMessage = "La fecha de creación es obligatoria")]
        [DataType(DataType.DateTime)]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Propiedades de navegación
        public virtual Inmueble Inmueble { get; set; } = null!;
        public virtual IdentityUser Usuario { get; set; } = null!;

        // Métodos de utilidad
        public bool EstaActiva()
        {
            return DateTime.Now < FechaExpiracion;
        }

        public static Reserva CrearReservaPor48Horas(int inmuebleId, string usuarioId)
        {
            var ahora = DateTime.Now;
            return new Reserva
            {
                InmuebleId = inmuebleId,
                UsuarioId = usuarioId,
                FechaCreacion = ahora,
                FechaExpiracion = ahora.AddHours(48)
            };
        }

        public TimeSpan TiempoRestante()
        {
            var tiempo = FechaExpiracion - DateTime.Now;
            return tiempo > TimeSpan.Zero ? tiempo : TimeSpan.Zero;
        }
    }
}