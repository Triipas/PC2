using PC2.Models;
using PC2.ViewModels;

namespace PC2.Services
{
    public interface IVisitasService
    {
        Task<(bool Exito, string Mensaje)> AgendarVisitaAsync(AgendarVisitaViewModel viewModel);
        Task<bool> TieneVisitaSolapadaAsync(int inmuebleId, DateTime fechaInicio, DateTime fechaFin, int? visitaId = null);
        Task<IEnumerable<Visita>> ObtenerVisitasDelUsuarioAsync(string usuarioId);
        Task<IEnumerable<Visita>> ObtenerVisitasDelInmuebleAsync(int inmuebleId);
        Task<Visita?> ObtenerVisitaPorIdAsync(int visitaId);
        Task<(bool Exito, string Mensaje)> CancelarVisitaAsync(int visitaId, string usuarioId);
        Task<IEnumerable<Visita>> ObtenerVisitasPorFechaAsync(DateTime fecha);
        Task<Dictionary<DateTime, int>> ObtenerVisitasPorDiaAsync(int inmuebleId, DateTime mesInicio);
    }
}