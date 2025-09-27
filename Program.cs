using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => 
{
    // Configuración de Identity más flexible para desarrollo
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
})
.AddRoles<IdentityRole>() // Agregar soporte para roles (necesario para Pregunta 5)
.AddEntityFrameworkStores<ApplicationDbContext>();

// Configurar Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? 
                           builder.Configuration.GetValue<string>("Redis:ConnectionString");

if (!string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        // Configurar Redis para caché distribuido
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName") ?? "PC2";
        });

        // Configurar conexión directa a Redis para operaciones avanzadas
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = ConfigurationOptions.Parse(redisConnectionString);
            configuration.AbortOnConnectFail = false; // No fallar si Redis no está disponible
            return ConnectionMultiplexer.Connect(configuration);
        });

        builder.Services.AddSingleton<ICacheService, CacheService>();
    }
    catch (Exception ex)
    {
        // Log error but continue without Redis
        Console.WriteLine($"Warning: Redis no disponible: {ex.Message}");
        
        // Usar caché en memoria como fallback
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<ICacheService, CacheService>();
    }
}
else
{
    // Usar caché en memoria si no hay Redis configurado
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ICacheService, CacheService>();
}

// Configurar sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("Session:TimeoutMinutes", 60));
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = builder.Configuration.GetValue<string>("Session:CookieName") ?? "PC2.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews();

// Registrar servicios de la aplicación
builder.Services.AddScoped<IVisitasService, VisitasService>();
builder.Services.AddScoped<IReservasService, ReservasService>();
builder.Services.AddScoped<ISessionService, SessionService>();

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Usar sesiones
app.UseSession();

// Seed data initialization
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedData.Initialize(services);
        
        // Limpiar reservas expiradas al inicio
        var reservasService = services.GetRequiredService<IReservasService>();
        var reservasLimpiadas = await reservasService.LimpiarReservasExpiradasAsync();
        if (reservasLimpiadas > 0)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Se limpiaron {Count} reservas expiradas al iniciar la aplicación", reservasLimpiadas);
        }

        // Verificar conexión a Redis
        var cacheService = services.GetRequiredService<ICacheService>();
        var redisConnected = await cacheService.IsConnectedAsync();
        var logger2 = services.GetRequiredService<ILogger<Program>>();
        
        if (redisConnected)
        {
            logger2.LogInformation("✅ Redis conectado exitosamente");
        }
        else
        {
            logger2.LogWarning("⚠️ Redis no disponible, usando caché en memoria");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();