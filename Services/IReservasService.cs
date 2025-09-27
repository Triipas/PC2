using PC2.Models;

namespace PC2.Services
{
    public interface IReservasService
    {
        Task<(bool Exito, string Mensaje)> ReservarInmuebleAsync(int inmuebleId, string usuarioId);
        Task<bool> TieneReservaActivaAsync(int inmuebleId);
        Task<Reserva?> ObtenerReservaActivaAsync(int inmuebleId);
        Task<IEnumerable<Reserva>> ObtenerReservasDelUsuarioAsync(string usuarioId);
        Task<(bool Exito, string Mensaje)> LiberarReservaAsync(int reservaId, string usuarioId);
        Task<int> LimpiarReservasExpiradasAsync();
        Task<IEnumerable<Reserva>> ObtenerReservasActivasAsync();
        Task<TimeSpan?> ObtenerTiempoRestanteReservaAsync(int inmuebleId);
    }
}