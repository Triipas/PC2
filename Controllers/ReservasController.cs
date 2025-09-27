using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Services;
using System.Security.Claims;

namespace PC2.Controllers
{
    [Authorize]
    public class ReservasController : Controller
    {
        private readonly IReservasService _reservasService;
        private readonly ILogger<ReservasController> _logger;

        public ReservasController(IReservasService reservasService, ILogger<ReservasController> logger)
        {
            _reservasService = reservasService;
            _logger = logger;
        }

        // POST: Reservas/Crear/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Challenge();
            }

            var resultado = await _reservasService.ReservarInmuebleAsync(id, usuarioId);

            if (resultado.Exito)
            {
                TempData["Success"] = resultado.Mensaje;
            }
            else
            {
                TempData["Error"] = resultado.Mensaje;
            }

            return RedirectToAction("Details", "Inmuebles", new { id });
        }

        // GET: Reservas/MisReservas
        public async Task<IActionResult> MisReservas()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Challenge();
            }

            var reservas = await _reservasService.ObtenerReservasDelUsuarioAsync(usuarioId);
            return View(reservas);
        }

        // POST: Reservas/Liberar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Liberar(int id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Challenge();
            }

            var resultado = await _reservasService.LiberarReservaAsync(id, usuarioId);

            if (resultado.Exito)
            {
                TempData["Success"] = resultado.Mensaje;
            }
            else
            {
                TempData["Error"] = resultado.Mensaje;
            }

            return RedirectToAction(nameof(MisReservas));
        }

        // API: Reservas/EstadoReserva/5
        [HttpGet]
        public async Task<IActionResult> EstadoReserva(int id)
        {
            var tiempoRestante = await _reservasService.ObtenerTiempoRestanteReservaAsync(id);
            var tieneReserva = await _reservasService.TieneReservaActivaAsync(id);

            if (!tieneReserva || !tiempoRestante.HasValue || tiempoRestante.Value <= TimeSpan.Zero)
            {
                return Json(new { 
                    reservado = false,
                    mensaje = "Disponible para reservar"
                });
            }

            var horas = (int)tiempoRestante.Value.TotalHours;
            var minutos = tiempoRestante.Value.Minutes;
            var segundos = tiempoRestante.Value.Seconds;

            return Json(new { 
                reservado = true,
                tiempoRestante = new {
                    horas,
                    minutos,
                    segundos,
                    totalSegundos = (int)tiempoRestante.Value.TotalSeconds
                },
                mensaje = $"Reservado por {horas}h {minutos}m {segundos}s más"
            });
        }

        // POST: Reservas/LimpiarExpiradas
        [HttpPost]
        public async Task<IActionResult> LimpiarExpiradas()
        {
            var cantidadLimpiadas = await _reservasService.LimpiarReservasExpiradasAsync();
            
            return Json(new { 
                exito = true,
                cantidadLimpiadas,
                mensaje = $"Se limpiaron {cantidadLimpiadas} reservas expiradas"
            });
        }
    }
}