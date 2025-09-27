using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;
using PC2.Services;

namespace PC2.Controllers
{
    [Authorize] // En la Pregunta 5 esto será [Authorize(Roles = "Broker")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ICacheService cacheService, ILogger<AdminController> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        // GET: Admin
        public async Task<IActionResult> Index()
        {
            var estadisticas = new
            {
                InmueblesActivos = await _context.Inmuebles.CountAsync(i => i.Activo),
                InmueblesInactivos = await _context.Inmuebles.CountAsync(i => !i.Activo),
                VisitasPendientes = await _context.Visitas.CountAsync(v => v.Estado == EstadoVisita.Solicitada),
                ReservasActivas = await _context.Reservas.CountAsync(r => r.FechaExpiracion > DateTime.Now),
                RedisConectado = await _cacheService.IsConnectedAsync(),
                ClavesEnCache = (await _cacheService.GetKeysAsync()).Length
            };

            ViewBag.Estadisticas = estadisticas;
            return View();
        }

        // GET: Admin/Inmuebles
        public async Task<IActionResult> Inmuebles()
        {
            var inmuebles = await _context.Inmuebles
                .OrderByDescending(i => i.Id)
                .ToListAsync();
            
            return View(inmuebles);
        }

        // GET: Admin/CrearInmueble
        public IActionResult CrearInmueble()
        {
            return View(new Inmueble());
        }

        // POST: Admin/CrearInmueble
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearInmueble(Inmueble inmueble)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Inmuebles.Add(inmueble);
                    await _context.SaveChangesAsync();

                    // Invalidar caché después de crear
                    await _cacheService.InvalidarCacheInmueblesAsync();
                    
                    TempData["Success"] = "Inmueble creado exitosamente";
                    _logger.LogInformation("Inmueble creado: {Codigo} - {Titulo}", inmueble.Codigo, inmueble.Titulo);
                    
                    return RedirectToAction(nameof(Inmuebles));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear inmueble");
                    ModelState.AddModelError("", "Error al crear el inmueble");
                }
            }

            return View(inmueble);
        }

        // GET: Admin/EditarInmueble/5
        public async Task<IActionResult> EditarInmueble(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inmueble = await _context.Inmuebles.FindAsync(id);
            if (inmueble == null)
            {
                return NotFound();
            }

            return View(inmueble);
        }

        // POST: Admin/EditarInmueble/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarInmueble(int id, Inmueble inmueble)
        {
            if (id != inmueble.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inmueble);
                    await _context.SaveChangesAsync();

                    // Invalidar caché después de editar
                    await _cacheService.InvalidarCacheInmueblesAsync();
                    
                    TempData["Success"] = "Inmueble actualizado exitosamente";
                    _logger.LogInformation("Inmueble editado: {Codigo} - {Titulo}", inmueble.Codigo, inmueble.Titulo);
                    
                    return RedirectToAction(nameof(Inmuebles));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InmuebleExists(inmueble.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al editar inmueble {Id}", id);
                    ModelState.AddModelError("", "Error al actualizar el inmueble");
                }
            }

            return View(inmueble);
        }

        // POST: Admin/CambiarEstado/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            try
            {
                var inmueble = await _context.Inmuebles.FindAsync(id);
                if (inmueble == null)
                {
                    return NotFound();
                }

                inmueble.Activo = !inmueble.Activo;
                await _context.SaveChangesAsync();

                // Invalidar caché después de cambiar estado
                await _cacheService.InvalidarCacheInmueblesAsync();
                
                TempData["Success"] = $"Inmueble {(inmueble.Activo ? "activado" : "desactivado")} exitosamente";
                _logger.LogInformation("Estado de inmueble cambiado: {Codigo} - Activo: {Activo}", inmueble.Codigo, inmueble.Activo);
                
                return RedirectToAction(nameof(Inmuebles));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del inmueble {Id}", id);
                TempData["Error"] = "Error al cambiar el estado del inmueble";
                return RedirectToAction(nameof(Inmuebles));
            }
        }

        // GET: Admin/Cache
        public async Task<IActionResult> Cache()
        {
            var claves = await _cacheService.GetKeysAsync();
            var estadisticas = new
            {
                RedisConectado = await _cacheService.IsConnectedAsync(),
                TotalClaves = claves.Length,
                ClavesInmuebles = claves.Where(k => k.StartsWith("inmuebles:")).ToArray(),
                ClavesCiudades = claves.Where(k => k.StartsWith("ciudades:")).ToArray(),
                OtrasClaves = claves.Where(k => !k.StartsWith("inmuebles:") && !k.StartsWith("ciudades:")).ToArray()
            };

            ViewBag.EstadisticasCache = estadisticas;
            return View();
        }

        // POST: Admin/LimpiarCache
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LimpiarCache(string? patron = null)
        {
            try
            {
                if (string.IsNullOrEmpty(patron))
                {
                    await _cacheService.FlushAllAsync();
                    TempData["Success"] = "Todo el caché ha sido limpiado";
                    _logger.LogInformation("Caché completamente limpiado");
                }
                else
                {
                    await _cacheService.RemovePatternAsync(patron);
                    TempData["Success"] = $"Caché limpiado para el patrón: {patron}";
                    _logger.LogInformation("Caché limpiado para patrón: {Patron}", patron);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al limpiar caché");
                TempData["Error"] = "Error al limpiar el caché";
            }

            return RedirectToAction(nameof(Cache));
        }

        // API: Admin/EstadisticasApi
        [HttpGet]
        public async Task<IActionResult> EstadisticasApi()
        {
            try
            {
                var claves = await _cacheService.GetKeysAsync();
                var estadisticas = new
                {
                    timestamp = DateTime.Now,
                    redis = new
                    {
                        conectado = await _cacheService.IsConnectedAsync(),
                        totalClaves = claves.Length,
                        clavesPorTipo = new
                        {
                            inmuebles = claves.Count(k => k.StartsWith("inmuebles:")),
                            ciudades = claves.Count(k => k.StartsWith("ciudades:")),
                            otros = claves.Count(k => !k.StartsWith("inmuebles:") && !k.StartsWith("ciudades:"))
                        }
                    },
                    database = new
                    {
                        inmueblesActivos = await _context.Inmuebles.CountAsync(i => i.Activo),
                        inmueblesInactivos = await _context.Inmuebles.CountAsync(i => !i.Activo),
                        visitasPendientes = await _context.Visitas.CountAsync(v => v.Estado == EstadoVisita.Solicitada),
                        reservasActivas = await _context.Reservas.CountAsync(r => r.FechaExpiracion > DateTime.Now)
                    }
                };

                return Json(estadisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas");
                return Json(new { error = ex.Message });
            }
        }

        private bool InmuebleExists(int id)
        {
            return _context.Inmuebles.Any(e => e.Id == id);
        }
    }
}