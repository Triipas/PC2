using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;

namespace PC2.Services
{
    public class ReservasService : IReservasService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReservasService> _logger;

        public ReservasService(ApplicationDbContext context, ILogger<ReservasService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Exito, string Mensaje)> ReservarInmuebleAsync(int inmuebleId, string usuarioId)
        {
            try
            {
                // Verificar que el inmueble existe y está activo
                var inmueble = await _context.Inmuebles.FindAsync(inmuebleId);
                if (inmueble == null || !inmueble.Activo)
                {
                    return (false, "El inmueble no existe o no está disponible.");
                }

                // Verificar que no hay reserva activa
                if (await TieneReservaActivaAsync(inmuebleId))
                {
                    var reservaExistente = await ObtenerReservaActivaAsync(inmuebleId);
                    var tiempoRestante = reservaExistente?.TiempoRestante();
                    
                    if (tiempoRestante.HasValue && tiempoRestante.Value > TimeSpan.Zero)
                    {
                        var horas = (int)tiempoRestante.Value.TotalHours;
                        var minutos = tiempoRestante.Value.Minutes;
                        return (false, $"Este inmueble ya está reservado. Tiempo restante: {horas}h {minutos}m");
                    }
                }

                // Limpiar reservas expiradas antes de crear una nueva
                await LimpiarReservasExpiradasAsync();

                // Crear nueva reserva
                var nuevaReserva = Reserva.CrearReservaPor48Horas(inmuebleId, usuarioId);
                _context.Reservas.Add(nuevaReserva);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reserva creada exitosamente. ID: {ReservaId}, Inmueble: {InmuebleId}, Usuario: {UsuarioId}", 
                    nuevaReserva.Id, inmuebleId, usuarioId);

                return (true, $"¡Inmueble reservado exitosamente por 48 horas! La reserva expira el {nuevaReserva.FechaExpiracion:dd/MM/yyyy 'a las' HH:mm}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reservar inmueble {InmuebleId} para usuario {UsuarioId}", inmuebleId, usuarioId);
                return (false, "Ocurrió un error al reservar el inmueble. Por favor, intenta nuevamente.");
            }
        }

        public async Task<bool> TieneReservaActivaAsync(int inmuebleId)
        {
            try
            {
                var ahora = DateTime.Now;
                return await _context.Reservas
                    .AnyAsync(r => r.InmuebleId == inmuebleId && r.FechaExpiracion > ahora);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar reserva activa para inmueble {InmuebleId}", inmuebleId);
                return true; // Por seguridad, asumimos que hay reserva si hay error
            }
        }

        public async Task<Reserva?> ObtenerReservaActivaAsync(int inmuebleId)
        {
            try
            {
                var ahora = DateTime.Now;
                return await _context.Reservas
                    .Include(r => r.Inmueble)
                    .Include(r => r.Usuario)
                    .FirstOrDefaultAsync(r => r.InmuebleId == inmuebleId && r.FechaExpiracion > ahora);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener reserva activa para inmueble {InmuebleId}", inmuebleId);
                return null;
            }
        }

        public async Task<IEnumerable<Reserva>> ObtenerReservasDelUsuarioAsync(string usuarioId)
        {
            try
            {
                return await _context.Reservas
                    .Include(r => r.Inmueble)
                    .Where(r => r.UsuarioId == usuarioId)
                    .OrderByDescending(r => r.FechaCreacion)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener reservas del usuario {UsuarioId}", usuarioId);
                return new List<Reserva>();
            }
        }

        public async Task<(bool Exito, string Mensaje)> LiberarReservaAsync(int reservaId, string usuarioId)
        {
            try
            {
                var reserva = await _context.Reservas.FindAsync(reservaId);
                
                if (reserva == null)
                {
                    return (false, "La reserva no existe.");
                }

                if (reserva.UsuarioId != usuarioId)
                {
                    return (false, "No tienes permisos para liberar esta reserva.");
                }

                if (!reserva.EstaActiva())
                {
                    return (false, "La reserva ya ha expirado.");
                }

                _context.Reservas.Remove(reserva);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reserva liberada. ID: {ReservaId}, Usuario: {UsuarioId}", reservaId, usuarioId);

                return (true, "Reserva liberada exitosamente. El inmueble está disponible nuevamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al liberar reserva {ReservaId}", reservaId);
                return (false, "Ocurrió un error al liberar la reserva.");
            }
        }

        public async Task<int> LimpiarReservasExpiradasAsync()
        {
            try
            {
                var ahora = DateTime.Now;
                var reservasExpiradas = await _context.Reservas
                    .Where(r => r.FechaExpiracion <= ahora)
                    .ToListAsync();

                if (reservasExpiradas.Any())
                {
                    _context.Reservas.RemoveRange(reservasExpiradas);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Se limpiaron {Cantidad} reservas expiradas", reservasExpiradas.Count);
                }

                return reservasExpiradas.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al limpiar reservas expiradas");
                return 0;
            }
        }

        public async Task<IEnumerable<Reserva>> ObtenerReservasActivasAsync()
        {
            try
            {
                var ahora = DateTime.Now;
                return await _context.Reservas
                    .Include(r => r.Inmueble)
                    .Include(r => r.Usuario)
                    .Where(r => r.FechaExpiracion > ahora)
                    .OrderBy(r => r.FechaExpiracion)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener reservas activas");
                return new List<Reserva>();
            }
        }

        public async Task<TimeSpan?> ObtenerTiempoRestanteReservaAsync(int inmuebleId)
        {
            try
            {
                var reserva = await ObtenerReservaActivaAsync(inmuebleId);
                return reserva?.TiempoRestante();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tiempo restante para inmueble {InmuebleId}", inmuebleId);
                return null;
            }
        }
    }
}