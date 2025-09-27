using PC2.ViewModels;
using System.Text.Json;

namespace PC2.Services
{
    public class SessionService : ISessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SessionService> _logger;
        
        private const string FILTROS_KEY = "filtros_inmuebles";
        private const string ULTIMO_INMUEBLE_KEY = "ultimo_inmueble";

        public SessionService(IHttpContextAccessor httpContextAccessor, ILogger<SessionService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private ISession? Session => _httpContextAccessor.HttpContext?.Session;

        public async Task GuardarFiltrosAsync(FiltrosInmueblesViewModel filtros)
        {
            try
            {
                if (Session == null) return;

                // Solo guardar si hay filtros activos
                if (filtros.TieneFiltrosActivos())
                {
                    await SetAsync(FILTROS_KEY, filtros);
                    _logger.LogDebug("Filtros guardados en sesión: {Filtros}", 
                        JsonSerializer.Serialize(new 
                        { 
                            filtros.Ciudad, 
                            filtros.Tipo, 
                            filtros.PrecioMin, 
                            filtros.PrecioMax, 
                            filtros.Dormitorios 
                        }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar filtros en sesión");
            }
        }

        public async Task<FiltrosInmueblesViewModel?> ObtenerFiltrosAsync()
        {
            try
            {
                var filtros = await GetAsync<FiltrosInmueblesViewModel>(FILTROS_KEY);
                if (filtros != null)
                {
                    _logger.LogDebug("Filtros recuperados de sesión");
                }
                return filtros;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener filtros de sesión");
                return null;
            }
        }

        public async Task LimpiarFiltrosAsync()
        {
            try
            {
                await RemoveAsync(FILTROS_KEY);
                _logger.LogDebug("Filtros limpiados de sesión");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al limpiar filtros de sesión");
            }
        }

        public async Task GuardarUltimoInmuebleAsync(int inmuebleId, string titulo)
        {
            try
            {
                if (Session == null) return;

                var ultimoInmueble = new { Id = inmuebleId, Titulo = titulo, FechaVisita = DateTime.Now };
                await SetAsync(ULTIMO_INMUEBLE_KEY, ultimoInmueble);
                _logger.LogDebug("Último inmueble guardado en sesión: {Id} - {Titulo}", inmuebleId, titulo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar último inmueble en sesión");
            }
        }

        public async Task<(int Id, string Titulo)?> ObtenerUltimoInmuebleAsync()
        {
            try
            {
                var ultimoInmueble = await GetAsync<dynamic>(ULTIMO_INMUEBLE_KEY);
                if (ultimoInmueble != null)
                {
                    var json = JsonSerializer.Serialize(ultimoInmueble);
                    var data = JsonSerializer.Deserialize<JsonElement>(json);
                    
                    JsonElement idElement;
                    JsonElement tituloElement;
                    bool hasId = data.TryGetProperty("Id", out idElement);
                    bool hasTitulo = data.TryGetProperty("Titulo", out tituloElement);

                    if (hasId && hasTitulo)
                    {
                        var id = idElement.GetInt32();
                        var titulo = tituloElement.GetString() ?? "";

                        _logger.LogDebug("Último inmueble recuperado de sesión: {Id} - {Titulo}", id, titulo);
                        return (id, titulo);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener último inmueble de sesión");
                return null;
            }
        }

        public async Task LimpiarUltimoInmuebleAsync()
        {
            try
            {
                await RemoveAsync(ULTIMO_INMUEBLE_KEY);
                _logger.LogDebug("Último inmueble limpiado de sesión");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al limpiar último inmueble de sesión");
            }
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                if (Session == null) return null;

                await Task.CompletedTask; // Para mantener la interfaz async
                var value = Session.GetString(key);
                if (string.IsNullOrEmpty(value))
                    return null;

                return JsonSerializer.Deserialize<T>(value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener de sesión la clave: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value) where T : class
        {
            try
            {
                if (Session == null) return;

                await Task.CompletedTask; // Para mantener la interfaz async
                var serializedValue = JsonSerializer.Serialize(value);
                Session.SetString(key, serializedValue);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar en sesión la clave: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                if (Session == null) return;

                await Task.CompletedTask; // Para mantener la interfaz async
                Session.Remove(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al eliminar de sesión la clave: {Key}", key);
            }
        }

        public async Task ClearAsync()
        {
            try
            {
                if (Session == null) return;

                await Task.CompletedTask; // Para mantener la interfaz async
                Session.Clear();
                _logger.LogDebug("Sesión completamente limpiada");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al limpiar sesión");
            }
        }

        public async Task<Dictionary<string, object>> ObtenerInfoSesionAsync()
        {
            try
            {
                if (Session == null) 
                    return new Dictionary<string, object> { { "disponible", false } };

                await Task.CompletedTask; // Para mantener la interfaz async

                var info = new Dictionary<string, object>
                {
                    { "disponible", true },
                    { "id", Session.Id },
                    { "claves", Session.Keys.ToArray() }
                };

                var filtros = await ObtenerFiltrosAsync();
                if (filtros != null)
                {
                    info.Add("filtros_activos", filtros.TieneFiltrosActivos());
                }

                var ultimoInmueble = await ObtenerUltimoInmuebleAsync();
                if (ultimoInmueble.HasValue)
                {
                    info.Add("ultimo_inmueble", new { ultimoInmueble.Value.Id, ultimoInmueble.Value.Titulo });
                }

                return info;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener información de sesión");
                return new Dictionary<string, object> { { "error", ex.Message } };
            }
        }
    }
}