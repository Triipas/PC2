using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;
using PC2.ViewModels;

namespace PC2.Controllers
{
    public class InmueblesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InmueblesController> _logger;

        public InmueblesController(ApplicationDbContext context, ILogger<InmueblesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Inmuebles (Catálogo)
        public async Task<IActionResult> Index(FiltrosInmueblesViewModel filtros)
        {
            // Validar modelo de filtros
            if (!ModelState.IsValid)
            {
                // Si hay errores de validación, mostrar la página con errores
                ViewData["ErroresValidacion"] = true;
            }

            try
            {
                var viewModel = new CatalogoViewModel
                {
                    Filtros = filtros
                };

                // Obtener ciudades disponibles para el dropdown
                viewModel.CiudadesDisponibles = await _context.Inmuebles
                    .Where(i => i.Activo)
                    .Select(i => i.Ciudad)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                // Construir query base (solo inmuebles activos)
                var query = _context.Inmuebles.Where(i => i.Activo);

                // Aplicar filtros solo si el modelo es válido
                if (ModelState.IsValid)
                {
                    query = AplicarFiltros(query, filtros);
                }

                // Contar total de items
                viewModel.TotalItems = await query.CountAsync();

                // Aplicar paginación
                var inmueblesPaginados = await query
                    .OrderBy(i => i.Precio)
                    .ThenBy(i => i.Titulo)
                    .Skip((filtros.Pagina - 1) * filtros.ItemsPorPagina)
                    .Take(filtros.ItemsPorPagina)
                    .ToListAsync();

                viewModel.Inmuebles = inmueblesPaginados;
                viewModel.CalcularPaginacion();
                viewModel.GenerarMensajeResultados();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el catálogo de inmuebles");
                TempData["Error"] = "Ocurrió un error al cargar los inmuebles. Por favor, intenta nuevamente.";
                
                return View(new CatalogoViewModel());
            }
        }

        // GET: Inmuebles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound("El inmueble solicitado no existe.");
            }

            try
            {
                var inmueble = await _context.Inmuebles
                    .FirstOrDefaultAsync(m => m.Id == id && m.Activo);

                if (inmueble == null)
                {
                    return NotFound("El inmueble solicitado no existe o no está disponible.");
                }

                // Obtener inmuebles similares (misma ciudad y tipo, excluyendo el actual)
                var inmueblesSimilares = await _context.Inmuebles
                    .Where(i => i.Activo && 
                               i.Id != id && 
                               i.Ciudad == inmueble.Ciudad && 
                               i.Tipo == inmueble.Tipo)
                    .OrderBy(i => Math.Abs(i.Precio - inmueble.Precio))
                    .Take(3)
                    .ToListAsync();

                ViewBag.InmueblesSimilares = inmueblesSimilares;
                
                return View(inmueble);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el detalle del inmueble {Id}", id);
                return NotFound("Ocurrió un error al cargar el inmueble.");
            }
        }

        private IQueryable<Inmueble> AplicarFiltros(IQueryable<Inmueble> query, FiltrosInmueblesViewModel filtros)
        {
            // Filtro por ciudad
            if (!string.IsNullOrWhiteSpace(filtros.Ciudad))
            {
                query = query.Where(i => i.Ciudad.Contains(filtros.Ciudad));
            }

            // Filtro por tipo
            if (filtros.Tipo.HasValue)
            {
                query = query.Where(i => i.Tipo == filtros.Tipo.Value);
            }

            // Filtro por precio mínimo
            if (filtros.PrecioMin.HasValue && filtros.PrecioMin.Value > 0)
            {
                query = query.Where(i => i.Precio >= filtros.PrecioMin.Value);
            }

            // Filtro por precio máximo
            if (filtros.PrecioMax.HasValue && filtros.PrecioMax.Value > 0)
            {
                query = query.Where(i => i.Precio <= filtros.PrecioMax.Value);
            }

            // Filtro por dormitorios
            if (filtros.Dormitorios.HasValue && filtros.Dormitorios.Value >= 0)
            {
                query = query.Where(i => i.Dormitorios >= filtros.Dormitorios.Value);
            }

            return query;
        }

        // POST: Inmuebles/LimpiarFiltros
        [HttpPost]
        public IActionResult LimpiarFiltros()
        {
            return RedirectToAction(nameof(Index));
        }
    }
}