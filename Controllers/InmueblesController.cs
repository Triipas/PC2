using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;
using PC2.ViewModels;
using PC2.Services;

namespace PC2.Controllers
{
    public class InmueblesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InmueblesController> _logger;
        private readonly ICacheService _cacheService;
        private readonly ISessionService _sessionService;

        public InmueblesController(
            ApplicationDbContext context, 
            ILogger<InmueblesController> logger,
            ICacheService cacheService,
            ISessionService sessionService)
        {
            _context = context;
            _logger = logger;
            _cacheService = cacheService;
            _sessionService = sessionService;
        }

        // GET: Inmuebles (Catálogo)
        public async Task<IActionResult> Index(FiltrosInmueblesViewModel? filtros)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool datosDesdeCache = false;

            try
            {
                // Si no se pasaron filtros, intentar recuperar de sesión
                if (filtros == null || !filtros.TieneFiltrosActivos())
                {
                    var filtrosSesion = await _sessionService.ObtenerFiltrosAsync();
                    if (filtrosSesion != null && filtrosSesion.TieneFiltrosActivos())
                    {
                        filtros = filtrosSesion;
                        _logger.LogInformation("Filtros recuperados desde sesión");
                    }
                }

                // Inicializar filtros si aún es null
                filtros ??= new FiltrosInmueblesViewModel();

                // Validar modelo de filtros
                if (!ModelState.IsValid)
                {
                    ViewData["ErroresValidacion"] = true;
                }

                var viewModel = new CatalogoViewModel
                {
                    Filtros = filtros
                };

                // Obtener ciudades disponibles para el dropdown
                viewModel.CiudadesDisponibles = await ObtenerCiudadesDisponiblesAsync();
                ViewBag.CiudadesDisponibles = viewModel.CiudadesDisponibles;

                // Generar clave de caché
                var claveCache = _cacheService.GenerarClaveInmuebles(
                    filtros.Ciudad, 
                    filtros.Tipo?.ToString(), 
                    filtros.PrecioMin, 
                    filtros.PrecioMax, 
                    filtros.Dormitorios, 
                    filtros.Pagina);

                // Intentar obtener desde caché
                var resultadoCache = await _cacheService.GetAsync<CatalogoResultadoCache>(claveCache);
                
                if (resultadoCache != null && ModelState.IsValid)
                {
                    // Datos desde caché
                    viewModel.Inmuebles = resultadoCache.Inmuebles;
                    viewModel.TotalItems = resultadoCache.TotalItems;
                    datosDesdeCache = true;
                    
                    _logger.LogInformation("Datos obtenidos desde caché. Clave: {Clave}", claveCache);
                }
                else
                {
                    // Consultar base de datos
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

                    // Guardar en caché por 60 segundos
                    if (ModelState.IsValid)
                    {
                        var resultadoParaCache = new CatalogoResultadoCache
                        {
                            Inmuebles = inmueblesPaginados,
                            TotalItems = viewModel.TotalItems,
                            FechaGeneracion = DateTime.Now
                        };

                        await _cacheService.SetAsync(claveCache, resultadoParaCache, TimeSpan.FromSeconds(60));
                    }

                    _logger.LogInformation("Datos obtenidos desde base de datos");
                }

                // Guardar filtros en sesión si están activos
                if (filtros.TieneFiltrosActivos())
                {
                    await _sessionService.GuardarFiltrosAsync(filtros);
                }

                viewModel.CalcularPaginacion();
                viewModel.GenerarMensajeResultados();

                stopwatch.Stop();
                ViewBag.TiempoCarga = stopwatch.ElapsedMilliseconds;
                ViewBag.DatosDesdeCache = datosDesdeCache;

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

                // Guardar en sesión como último inmueble visitado
                await _sessionService.GuardarUltimoInmuebleAsync(inmueble.Id, inmueble.Titulo);

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

        // POST: Inmuebles/LimpiarFiltros
        [HttpPost]
        public async Task<IActionResult> LimpiarFiltros()
        {
            await _sessionService.LimpiarFiltrosAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Inmuebles/EstadisticasCache
        public async Task<IActionResult> EstadisticasCache()
        {
            try
            {
                var claves = await _cacheService.GetKeysAsync("inmuebles:*");
                var estadisticas = new
                {
                    TotalClaves = claves.Length,
                    Claves = claves,
                    RedisConectado = await _cacheService.IsConnectedAsync(),
                    InfoSesion = await _sessionService.ObtenerInfoSesionAsync()
                };

                return Json(estadisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de caché");
                return Json(new { error = ex.Message });
            }
        }

        private async Task<IEnumerable<string>> ObtenerCiudadesDisponiblesAsync()
        {
            const string claveCiudades = "ciudades:disponibles";
            
            // Intentar obtener desde caché
            var ciudadesCache = await _cacheService.GetAsync<List<string>>(claveCiudades);
            if (ciudadesCache != null)
            {
                return ciudadesCache;
            }

            // Consultar base de datos
            var ciudades = await _context.Inmuebles
                .Where(i => i.Activo)
                .Select(i => i.Ciudad)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Guardar en caché por 5 minutos (las ciudades cambian poco)
            await _cacheService.SetAsync(claveCiudades, ciudades, TimeSpan.FromMinutes(5));

            return ciudades;
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
    }

    // Clase para el resultado del caché
    public class CatalogoResultadoCache
    {
        public IEnumerable<Inmueble> Inmuebles { get; set; } = new List<Inmueble>();
        public int TotalItems { get; set; }
        public DateTime FechaGeneracion { get; set; }
    }
}