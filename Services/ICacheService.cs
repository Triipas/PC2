namespace PC2.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
        Task RemoveAsync(string key);
        Task RemovePatternAsync(string pattern);
        Task<bool> ExistsAsync(string key);
        Task<string[]> GetKeysAsync(string pattern = "*");
        Task FlushAllAsync();
        
        // Métodos específicos para inmuebles
        string GenerarClaveInmuebles(string? ciudad, string? tipo, decimal? precioMin, decimal? precioMax, int? dormitorios, int pagina);
        Task InvalidarCacheInmueblesAsync();
        Task<bool> IsConnectedAsync();
    }
}