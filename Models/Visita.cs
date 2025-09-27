using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PC2.Models
{
    public class Visita
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El inmueble es obligatorio")]
        public int InmuebleId { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio")]
        public string UsuarioId { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
        [DataType(DataType.DateTime)]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha de fin es obligatoria")]
        [DataType(DataType.DateTime)]
        public DateTime FechaFin { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        public EstadoVisita Estado { get; set; } = EstadoVisita.Solicitada;

        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
        public string? Notas { get; set; }

        // Propiedades de navegación
        public virtual Inmueble Inmueble { get; set; } = null!;
        public virtual IdentityUser Usuario { get; set; } = null!;

        // Validación personalizada
        public bool EsFechaValida()
        {
            return FechaInicio < FechaFin;
        }

        public bool EsHorarioLaboral()
        {
            var horaInicio = FechaInicio.TimeOfDay;
            var horaFin = FechaFin.TimeOfDay;
            
            return horaInicio >= new TimeSpan(8, 0, 0) && 
                   horaFin <= new TimeSpan(19, 0, 0);
        }

        public bool SeSolapaCon(Visita otra)
        {
            return InmuebleId == otra.InmuebleId &&
                   FechaInicio < otra.FechaFin &&
                   FechaFin > otra.FechaInicio;
        }
    }
}