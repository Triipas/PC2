using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Services;
using PC2.ViewModels;
using System.Security.Claims;

namespace PC2.Controllers
{
    [Authorize]
    public class VisitasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVisitasService _visitasService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<VisitasController> _logger;

        public VisitasController(
            ApplicationDbContext context, 
            IVisitasService visitasService, 
            UserManager<IdentityUser> userManager,
            ILogger<VisitasController> logger)
        {
            _context = context;
            _visitasService = visitasService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Visitas/Agendar/5
        public async Task<IActionResult> Agendar(int id)
        {
            var inmueble = await _context.Inmuebles.FindAsync(id);
            if (inmueble == null || !inmueble.Activo)
            {
                TempData["Error"] = "El inmueble no existe o no está disponible.";
                return RedirectToAction("Index", "Inmuebles");
            }

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Challenge();
            }

            var viewModel = new AgendarVisitaViewModel
            {
                InmuebleId = id,
                InmuebleTitulo = inmueble.Titulo,
                UsuarioId = usuarioId,
                Inmueble = inmueble,
                FechaInicio = DateTime.Now.Date.AddDays(1).AddHours(9), // Mañana a las 9 AM
                FechaFin = DateTime.Now.Date.AddDays(1).AddHours(10)     // Mañana a las 10 AM
            };

            // Obtener visitas existentes para mostrar disponibilidad
            var visitasExistentes = await _visitasService.ObtenerVisitasDelInmuebleAsync(id);
            ViewBag.VisitasExistentes = visitasExistentes.Where(v => v.FechaInicio >= DateTime.Now && v.Estado != Models.EstadoVisita.Cancelada);

            return View(viewModel);
        }

        // POST: Visitas/Agendar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agendar(AgendarVisitaViewModel viewModel)
        {
            // Verificar usuario autenticado
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Challenge();
            }

            viewModel.UsuarioId = usuarioId;

            // Cargar inmueble para mostrar en caso de error
            var inmueble = await _context.Inmuebles.FindAsync(viewModel.InmuebleId);
            if (inmueble == null || !inmueble.Activo)
            {
                TempData["Error"] = "El inmueble no existe o no está disponible.";
                return RedirectToAction("Index", "Inmuebles");
            }

            viewModel.Inmueble = inmueble;
            viewModel.InmuebleTitulo = inmueble.Titulo;

            if (!ModelState.IsValid)
            {
                // Recargar datos para la vista
                var visitasExistentes = await _visitasService.ObtenerVisitasDelInmuebleAsync(viewModel.InmuebleId);
                ViewBag.VisitasExistentes = visitasExistentes.Where(v => v.FechaInicio >= DateTime.Now && v.Estado != Models.EstadoVisita.Cancelada);
                
                return View(viewModel);
            }

            // Intentar agendar la visita
            var resultado = await _visitasService.AgendarVisitaAsync(viewModel);

            if (resultado.Exito)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction("Details", "Inmuebles", new { id = viewModel.InmuebleId });
            }
            else
            {
                ModelState.AddModelError("", resultado.Mensaje);
                
                // Recargar datos para la vista
                var visitasExistentes = await _visitasService.ObtenerVisitasDelInmuebleAsync(viewModel.InmuebleId);
                ViewBag.VisitasExistentes = visitasExistentes.Where(v => v.FechaInicio >= DateTime.Now && v.Estado != Models.EstadoVisita.Cancelada);
                
                return View(viewModel);
            }
        }

        // GET: Visitas/MisVisitas
        public async Task<IActionResult> MisVisitas()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Challenge();
            }

            var visitas = await _visitasService.ObtenerVisitasDelUsuarioAsync(usuarioId);
            return View(visitas);
        }

        // POST: Visitas/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Challenge();
            }

            var resultado = await _visitasService.CancelarVisitaAsync(id, usuarioId);

            if (resultado.Exito)
            {
                TempData["Success"] = resultado.Mensaje;
            }
            else
            {
                TempData["Error"] = resultado.Mensaje;
            }

            return RedirectToAction(nameof(MisVisitas));
        }

        // API: Visitas/VerificarDisponibilidad
        [HttpPost]
        public async Task<IActionResult> VerificarDisponibilidad([FromBody] VerificarDisponibilidadRequest request)
        {
            if (request.InmuebleId <= 0 || request.FechaInicio >= request.FechaFin)
            {
                return Json(new { disponible = false, mensaje = "Parámetros inválidos" });
            }

            var tieneSolapamiento = await _visitasService.TieneVisitaSolapadaAsync(
                request.InmuebleId, 
                request.FechaInicio, 
                request.FechaFin);

            if (tieneSolapamiento)
            {
                return Json(new { 
                    disponible = false, 
                    mensaje = "Ya hay una visita programada en ese horario" 
                });
            }

            return Json(new { 
                disponible = true, 
                mensaje = "Horario disponible" 
            });
        }

        // GET: Visitas/Calendario/5
        public async Task<IActionResult> Calendario(int id)
        {
            var inmueble = await _context.Inmuebles.FindAsync(id);
            if (inmueble == null || !inmueble.Activo)
            {
                return NotFound();
            }

            var mesActual = DateTime.Now.Date.AddDays(-DateTime.Now.Day + 1); // Primer día del mes actual
            var visitasPorDia = await _visitasService.ObtenerVisitasPorDiaAsync(id, mesActual);

            ViewBag.Inmueble = inmueble;
            ViewBag.VisitasPorDia = visitasPorDia;
            ViewBag.MesActual = mesActual;

            return View();
        }
    }

    // Clase para el request de verificar disponibilidad
    public class VerificarDisponibilidadRequest
    {
        public int InmuebleId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }
}