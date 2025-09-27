using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    public class Inmueble
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(20, ErrorMessage = "El código no puede exceder 20 caracteres")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(200, ErrorMessage = "El título no puede exceder 200 caracteres")]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La URL de la imagen no puede exceder 500 caracteres")]
        public string? Imagen { get; set; }

        [Required(ErrorMessage = "El tipo de inmueble es obligatorio")]
        public TipoInmueble Tipo { get; set; }

        [Required(ErrorMessage = "La ciudad es obligatoria")]
        [StringLength(100, ErrorMessage = "La ciudad no puede exceder 100 caracteres")]
        public string Ciudad { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [StringLength(300, ErrorMessage = "La dirección no puede exceder 300 caracteres")]
        public string Direccion { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Los dormitorios no pueden ser negativos")]
        public int Dormitorios { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Los baños no pueden ser negativos")]
        public int Banos { get; set; }

        [Required(ErrorMessage = "Los metros cuadrados son obligatorios")]
        [Range(1, double.MaxValue, ErrorMessage = "Los metros cuadrados deben ser mayor a 0")]
        public decimal MetrosCuadrados { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Precio { get; set; }

        public bool Activo { get; set; } = true;

        // Propiedades de navegación
        public virtual ICollection<Visita> Visitas { get; set; } = new List<Visita>();
        public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}