using PC2.Models;

namespace PC2.ViewModels
{
    public class CatalogoViewModel
    {
        public IEnumerable<Inmueble> Inmuebles { get; set; } = new List<Inmueble>();
        public FiltrosInmueblesViewModel Filtros { get; set; } = new FiltrosInmueblesViewModel();
        
        // Información de paginación
        public int TotalItems { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public bool TienePaginaAnterior => PaginaActual > 1;
        public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
        
        // Listas para los dropdowns
        public IEnumerable<string> CiudadesDisponibles { get; set; } = new List<string>();
        public IEnumerable<TipoInmueble> TiposDisponibles { get; set; } = Enum.GetValues<TipoInmueble>();
        
        // Información adicional
        public string MensajeResultados { get; set; } = string.Empty;
        
        // Métodos de utilidad
        public void CalcularPaginacion()
        {
            TotalPaginas = (int)Math.Ceiling((double)TotalItems / Filtros.ItemsPorPagina);
            PaginaActual = Filtros.Pagina;
        }
        
        public void GenerarMensajeResultados()
        {
            if (TotalItems == 0)
            {
                MensajeResultados = "No se encontraron inmuebles con los filtros seleccionados.";
            }
            else if (TotalItems == 1)
            {
                MensajeResultados = "Se encontró 1 inmueble.";
            }
            else if (Filtros.TieneFiltrosActivos())
            {
                MensajeResultados = $"Se encontraron {TotalItems} inmuebles que coinciden con tu búsqueda.";
            }
            else
            {
                MensajeResultados = $"Mostrando todos los inmuebles disponibles ({TotalItems} total).";
            }
        }
        
        // Información para navegación de páginas
        public IEnumerable<int> PaginasParaMostrar()
        {
            var paginas = new List<int>();
            var inicio = Math.Max(1, PaginaActual - 2);
            var fin = Math.Min(TotalPaginas, PaginaActual + 2);
            
            for (int i = inicio; i <= fin; i++)
            {
                paginas.Add(i);
            }
            
            return paginas;
        }
    }
}