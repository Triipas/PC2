using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;
using PC2.ViewModels;

namespace PC2.Services
{
    public class VisitasService : IVisitasService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VisitasService> _logger;

        public VisitasService(ApplicationDbContext context, ILogger<VisitasService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Exito, string Mensaje)> AgendarVisitaAsync(AgendarVisitaViewModel viewModel)
        {
            try
            {
                // Verificar que el inmueble existe y está activo
                var inmueble = await _context.Inmuebles.FindAsync(viewModel.InmuebleId);
                if (inmueble == null || !inmueble.Activo)
                {
                    return (false, "El inmueble no existe o no está disponible.");
                }

                // Verificar si hay visitas solapadas
                if (await TieneVisitaSolapadaAsync(viewModel.InmuebleId, viewModel.FechaInicio, viewModel.FechaFin))
                {
                    return (false, "Ya existe una visita programada en ese horario. Por favor, selecciona otro horario.");
                }

                // Crear la nueva visita
                var nuevaVisita = viewModel.ToVisita();
                _context.Visitas.Add(nuevaVisita);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Visita agendada exitosamente. ID: {VisitaId}, Inmueble: {InmuebleId}, Usuario: {UsuarioId}", 
                    nuevaVisita.Id, viewModel.InmuebleId, viewModel.UsuarioId);

                return (true, $"¡Visita agendada exitosamente! Tu cita es el {viewModel.FechaInicio:dd/MM/yyyy 'a las' HH:mm}. Recibirás una confirmación por email.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agendar visita para inmueble {InmuebleId}", viewModel.InmuebleId);
                return (false, "Ocurrió un error al agendar la visita. Por favor, intenta nuevamente.");
            }
        }

        public async Task<bool> TieneVisitaSolapadaAsync(int inmuebleId, DateTime fechaInicio, DateTime fechaFin, int? visitaId = null)
        {
            try
            {
                var query = _context.Visitas.Where(v => 
                    v.InmuebleId == inmuebleId &&
                    v.Estado != EstadoVisita.Cancelada &&
                    v.FechaInicio < fechaFin &&
                    v.FechaFin > fechaInicio);

                // Excluir la visita actual si se está editando
                if (visitaId.HasValue)
                {
                    query = query.Where(v => v.Id != visitaId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar solapamiento de visitas para inmueble {InmuebleId}", inmuebleId);
                return true; // Por seguridad, asumimos que hay solapamiento si hay error
            }
        }

        public async Task<IEnumerable<Visita>> ObtenerVisitasDelUsuarioAsync(string usuarioId)
        {
            try
            {
                return await _context.Visitas
                    .Include(v => v.Inmueble)
                    .Where(v => v.UsuarioId == usuarioId)
                    .OrderByDescending(v => v.FechaInicio)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener visitas del usuario {UsuarioId}", usuarioId);
                return new List<Visita>();
            }
        }

        public async Task<IEnumerable<Visita>> ObtenerVisitasDelInmuebleAsync(int inmuebleId)
        {
            try
            {
                return await _context.Visitas
                    .Include(v => v.Usuario)
                    .Where(v => v.InmuebleId == inmuebleId)
                    .OrderBy(v => v.FechaInicio)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener visitas del inmueble {InmuebleId}", inmuebleId);
                return new List<Visita>();
            }
        }

        public async Task<Visita?> ObtenerVisitaPorIdAsync(int visitaId)
        {
            try
            {
                return await _context.Visitas
                    .Include(v => v.Inmueble)
                    .Include(v => v.Usuario)
                    .FirstOrDefaultAsync(v => v.Id == visitaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener visita {VisitaId}", visitaId);
                return null;
            }
        }

        public async Task<(bool Exito, string Mensaje)> CancelarVisitaAsync(int visitaId, string usuarioId)
        {
            try
            {
                var visita = await _context.Visitas.FindAsync(visitaId);
                
                if (visita == null)
                {
                    return (false, "La visita no existe.");
                }

                if (visita.UsuarioId != usuarioId)
                {
                    return (false, "No tienes permisos para cancelar esta visita.");
                }

                if (visita.Estado == EstadoVisita.Cancelada)
                {
                    return (false, "La visita ya está cancelada.");
                }

                if (visita.FechaInicio <= DateTime.Now)
                {
                    return (false, "No se puede cancelar una visita que ya comenzó.");
                }

                visita.Estado = EstadoVisita.Cancelada;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Visita cancelada. ID: {VisitaId}, Usuario: {UsuarioId}", visitaId, usuarioId);

                return (true, "Visita cancelada exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar visita {VisitaId}", visitaId);
                return (false, "Ocurrió un error al cancelar la visita.");
            }
        }

        public async Task<IEnumerable<Visita>> ObtenerVisitasPorFechaAsync(DateTime fecha)
        {
            try
            {
                var inicioDelDia = fecha.Date;
                var finDelDia = inicioDelDia.AddDays(1);

                return await _context.Visitas
                    .Include(v => v.Inmueble)
                    .Include(v => v.Usuario)
                    .Where(v => v.FechaInicio >= inicioDelDia && v.FechaInicio < finDelDia)
                    .OrderBy(v => v.FechaInicio)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener visitas por fecha {Fecha}", fecha);
                return new List<Visita>();
            }
        }

        public async Task<Dictionary<DateTime, int>> ObtenerVisitasPorDiaAsync(int inmuebleId, DateTime mesInicio)
        {
            try
            {
                var mesFin = mesInicio.AddMonths(1);
                
                var visitas = await _context.Visitas
                    .Where(v => v.InmuebleId == inmuebleId && 
                               v.FechaInicio >= mesInicio && 
                               v.FechaInicio < mesFin &&
                               v.Estado != EstadoVisita.Cancelada)
                    .GroupBy(v => v.FechaInicio.Date)
                    .Select(g => new { Fecha = g.Key, Cantidad = g.Count() })
                    .ToListAsync();

                return visitas.ToDictionary(v => v.Fecha, v => v.Cantidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener visitas por día para inmueble {InmuebleId}", inmuebleId);
                return new Dictionary<DateTime, int>();
            }
        }
    }
}