using System.ComponentModel.DataAnnotations;
using PC2.Models;

namespace PC2.ViewModels
{
    public class FiltrosInmueblesViewModel : IValidatableObject
    {
        [Display(Name = "Ciudad")]
        public string? Ciudad { get; set; }

        [Display(Name = "Tipo de Inmueble")]
        public TipoInmueble? Tipo { get; set; }

        [Display(Name = "Precio Mínimo")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio mínimo no puede ser negativo")]
        [DataType(DataType.Currency)]
        public decimal? PrecioMin { get; set; }

        [Display(Name = "Precio Máximo")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio máximo no puede ser negativo")]
        [DataType(DataType.Currency)]
        public decimal? PrecioMax { get; set; }

        [Display(Name = "Mínimo de Dormitorios")]
        [Range(0, int.MaxValue, ErrorMessage = "Los dormitorios no pueden ser negativos")]
        public int? Dormitorios { get; set; }

        [Display(Name = "Página")]
        [Range(1, int.MaxValue, ErrorMessage = "La página debe ser mayor a 0")]
        public int Pagina { get; set; } = 1;

        public int ItemsPorPagina { get; set; } = 10;

        // Validación personalizada para el rango de precios
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (PrecioMin.HasValue && PrecioMax.HasValue && PrecioMin.Value > PrecioMax.Value)
            {
                results.Add(new ValidationResult(
                    "El precio mínimo no puede ser mayor al precio máximo",
                    new[] { nameof(PrecioMin), nameof(PrecioMax) }));
            }

            if (Pagina < 1)
            {
                results.Add(new ValidationResult(
                    "La página debe ser mayor a 0",
                    new[] { nameof(Pagina) }));
            }

            if (Dormitorios.HasValue && Dormitorios.Value < 0)
            {
                results.Add(new ValidationResult(
                    "Los dormitorios no pueden ser negativos",
                    new[] { nameof(Dormitorios) }));
            }

            return results;
        }

        // Método para limpiar filtros
        public void LimpiarFiltros()
        {
            Ciudad = null;
            Tipo = null;
            PrecioMin = null;
            PrecioMax = null;
            Dormitorios = null;
            Pagina = 1;
        }

        // Método para verificar si hay filtros activos
        public bool TieneFiltrosActivos()
        {
            return !string.IsNullOrWhiteSpace(Ciudad) ||
                   Tipo.HasValue ||
                   PrecioMin.HasValue ||
                   PrecioMax.HasValue ||
                   Dormitorios.HasValue;
        }
    }
}