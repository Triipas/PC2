using System.ComponentModel.DataAnnotations;
using PC2.Models;

namespace PC2.ViewModels
{
    public class AgendarVisitaViewModel : IValidatableObject
    {
        public int InmuebleId { get; set; }
        public string InmuebleTitulo { get; set; } = string.Empty;
        public string UsuarioId { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha y hora de inicio es obligatoria")]
        [Display(Name = "Fecha y hora de inicio")]
        [DataType(DataType.DateTime)]
        public DateTime FechaInicio { get; set; } = DateTime.Now.Date.AddHours(9); // Default 9:00 AM

        [Required(ErrorMessage = "La fecha y hora de fin es obligatoria")]
        [Display(Name = "Fecha y hora de fin")]
        [DataType(DataType.DateTime)]
        public DateTime FechaFin { get; set; } = DateTime.Now.Date.AddHours(10); // Default 10:00 AM

        [Display(Name = "Notas adicionales")]
        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
        [DataType(DataType.MultilineText)]
        public string? Notas { get; set; }

        // Propiedades auxiliares para mostrar información del inmueble
        public Inmueble? Inmueble { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validar que la fecha de inicio sea menor que la de fin
            if (FechaInicio >= FechaFin)
            {
                results.Add(new ValidationResult(
                    "La fecha de inicio debe ser anterior a la fecha de fin",
                    new[] { nameof(FechaInicio), nameof(FechaFin) }));
            }

            // Validar que las fechas no sean en el pasado
            var ahora = DateTime.Now;
            if (FechaInicio <= ahora)
            {
                results.Add(new ValidationResult(
                    "La fecha de inicio debe ser en el futuro",
                    new[] { nameof(FechaInicio) }));
            }

            // Validar horario laboral (8:00 AM - 7:00 PM)
            var horaInicio = FechaInicio.TimeOfDay;
            var horaFin = FechaFin.TimeOfDay;
            var horarioInicioLaboral = new TimeSpan(8, 0, 0);  // 8:00 AM
            var horarioFinLaboral = new TimeSpan(19, 0, 0);    // 7:00 PM

            if (horaInicio < horarioInicioLaboral || horaInicio >= horarioFinLaboral)
            {
                results.Add(new ValidationResult(
                    "La hora de inicio debe estar entre 8:00 AM y 7:00 PM",
                    new[] { nameof(FechaInicio) }));
            }

            if (horaFin <= horarioInicioLaboral || horaFin > horarioFinLaboral)
            {
                results.Add(new ValidationResult(
                    "La hora de fin debe estar entre 8:00 AM y 7:00 PM",
                    new[] { nameof(FechaFin) }));
            }

            // Validar que la visita no sea en domingo
            if (FechaInicio.DayOfWeek == DayOfWeek.Sunday)
            {
                results.Add(new ValidationResult(
                    "No se pueden agendar visitas los domingos",
                    new[] { nameof(FechaInicio) }));
            }

            // Validar duración mínima y máxima (30 min - 3 horas)
            var duracion = FechaFin - FechaInicio;
            if (duracion.TotalMinutes < 30)
            {
                results.Add(new ValidationResult(
                    "La visita debe durar al menos 30 minutos",
                    new[] { nameof(FechaFin) }));
            }

            if (duracion.TotalHours > 3)
            {
                results.Add(new ValidationResult(
                    "La visita no puede durar más de 3 horas",
                    new[] { nameof(FechaFin) }));
            }

            // Validar que no sea más de 30 días en el futuro
            if (FechaInicio > DateTime.Now.AddDays(30))
            {
                results.Add(new ValidationResult(
                    "No se pueden agendar visitas con más de 30 días de anticipación",
                    new[] { nameof(FechaInicio) }));
            }

            return results;
        }

        // Método para convertir a entidad Visita
        public Visita ToVisita()
        {
            return new Visita
            {
                InmuebleId = InmuebleId,
                UsuarioId = UsuarioId,
                FechaInicio = FechaInicio,
                FechaFin = FechaFin,
                Notas = Notas,
                Estado = EstadoVisita.Solicitada
            };
        }

        // Método para obtener slots de tiempo sugeridos
        public static List<(DateTime Inicio, DateTime Fin, string Descripcion)> ObtenerSlotsDisponibles(DateTime fecha)
        {
            var slots = new List<(DateTime, DateTime, string)>();
            
            var fechaBase = fecha.Date;
            
            // Slots de mañana
            slots.Add((fechaBase.AddHours(9), fechaBase.AddHours(10), "9:00 AM - 10:00 AM (Mañana)"));
            slots.Add((fechaBase.AddHours(10), fechaBase.AddHours(11), "10:00 AM - 11:00 AM (Mañana)"));
            slots.Add((fechaBase.AddHours(11), fechaBase.AddHours(12), "11:00 AM - 12:00 PM (Mañana)"));
            
            // Slots de tarde
            slots.Add((fechaBase.AddHours(14), fechaBase.AddHours(15), "2:00 PM - 3:00 PM (Tarde)"));
            slots.Add((fechaBase.AddHours(15), fechaBase.AddHours(16), "3:00 PM - 4:00 PM (Tarde)"));
            slots.Add((fechaBase.AddHours(16), fechaBase.AddHours(17), "4:00 PM - 5:00 PM (Tarde)"));
            slots.Add((fechaBase.AddHours(17), fechaBase.AddHours(18), "5:00 PM - 6:00 PM (Tarde)"));

            return slots;
        }
    }
}