using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using StackExchange.Redis;

namespace PC2.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConnectionMultiplexer? _redis;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CacheService> _logger;
        private readonly int _defaultCacheTimeMinutes;

        public CacheService(
            IDistributedCache distributedCache,
            IConnectionMultiplexer? redis,
            IConfiguration configuration,
            ILogger<CacheService> logger)
        {
            _distributedCache = distributedCache;
            _redis = redis;
            _configuration = configuration;
            _logger = logger;
            _defaultCacheTimeMinutes = configuration.GetValue<int>("Redis:DefaultCacheTimeMinutes", 1);
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var value = await _distributedCache.GetStringAsync(key);
                if (string.IsNullOrEmpty(value))
                    return null;

                var result = JsonSerializer.Deserialize<T>(value);
                _logger.LogDebug("Cache HIT para clave: {Key}", key);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener del cache la clave: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value);
                var options = new DistributedCacheEntryOptions();

                if (expiry.HasValue)
                {
                    options.SetAbsoluteExpiration(expiry.Value);
                }
                else
                {
                    options.SetAbsoluteExpiration(TimeSpan.FromMinutes(_defaultCacheTimeMinutes));
                }

                await _distributedCache.SetStringAsync(key, serializedValue, options);
                _logger.LogDebug("Cache SET para clave: {Key}, Expiración: {Expiry}", key, 
                    expiry ?? TimeSpan.FromMinutes(_defaultCacheTimeMinutes));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar en cache la clave: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _distributedCache.RemoveAsync(key);
                _logger.LogDebug("Cache REMOVE para clave: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al eliminar del cache la clave: {Key}", key);
            }
        }

        public async Task RemovePatternAsync(string pattern)
        {
            try
            {
                if (_redis == null)
                {
                    _logger.LogWarning("Redis no disponible para RemovePatternAsync");
                    return;
                }

                var database = _redis.GetDatabase();
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                
                var keys = server.Keys(pattern: pattern).ToArray();
                if (keys.Length > 0)
                {
                    await database.KeyDeleteAsync(keys);
                    _logger.LogDebug("Cache REMOVE PATTERN: {Pattern}, {Count} claves eliminadas", pattern, keys.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al eliminar del cache el patrón: {Pattern}", pattern);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var value = await _distributedCache.GetStringAsync(key);
                return !string.IsNullOrEmpty(value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al verificar existencia en cache de la clave: {Key}", key);
                return false;
            }
        }

        public async Task<string[]> GetKeysAsync(string pattern = "*")
        {
            try
            {
                if (_redis == null)
                {
                    _logger.LogWarning("Redis no disponible para GetKeysAsync");
                    return Array.Empty<string>();
                }

                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keysAsync = server.KeysAsync(pattern: pattern);
                var keysList = new List<string>();
                await foreach (var key in keysAsync)
                {
                    keysList.Add(key.ToString());
                }
                return keysList.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener claves del cache con patrón: {Pattern}", pattern);
                return Array.Empty<string>();
            }
        }

        public async Task FlushAllAsync()
        {
            try
            {
                if (_redis == null)
                {
                    _logger.LogWarning("Redis no disponible para FlushAllAsync");
                    return;
                }

                var server = _redis.GetServer(_redis.GetEndPoints().First());
                await server.FlushAllDatabasesAsync();
                _logger.LogInformation("Cache completamente limpiado (FlushAll)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al limpiar todo el cache");
            }
        }

        public string GenerarClaveInmuebles(string? ciudad, string? tipo, decimal? precioMin, decimal? precioMax, int? dormitorios, int pagina)
        {
            var filtros = new
            {
                ciudad = ciudad?.ToLowerInvariant() ?? "all",
                tipo = tipo?.ToLowerInvariant() ?? "all",
                precioMin = precioMin ?? 0,
                precioMax = precioMax ?? 0,
                dormitorios = dormitorios ?? -1,
                pagina
            };

            var filtrosJson = JsonSerializer.Serialize(filtros);
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(filtrosJson)));
            
            return $"inmuebles:filtros:{hash}";
        }

        public async Task InvalidarCacheInmueblesAsync()
        {
            try
            {
                await RemovePatternAsync("inmuebles:*");
                _logger.LogInformation("Cache de inmuebles invalidado");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al invalidar cache de inmuebles");
            }
        }

        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                if (_redis == null)
                    return false;

                var database = _redis.GetDatabase();
                await database.PingAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis no está conectado");
                return false;
            }
        }
    }
}