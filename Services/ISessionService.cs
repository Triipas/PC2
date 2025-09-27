using PC2.ViewModels;

namespace PC2.Services
{
    public interface ISessionService
    {
        // Gestión de filtros
        Task GuardarFiltrosAsync(FiltrosInmueblesViewModel filtros);
        Task<FiltrosInmueblesViewModel?> ObtenerFiltrosAsync();
        Task LimpiarFiltrosAsync();

        // Gestión de último inmueble visitado
        Task GuardarUltimoInmuebleAsync(int inmuebleId, string titulo);
        Task<(int Id, string Titulo)?> ObtenerUltimoInmuebleAsync();
        Task LimpiarUltimoInmuebleAsync();

        // Gestión general de sesión
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value) where T : class;
        Task RemoveAsync(string key);
        Task ClearAsync();

        // Información de sesión
        Task<Dictionary<string, object>> ObtenerInfoSesionAsync();
    }
}