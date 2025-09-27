using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Services;

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

builder.Services.AddControllersWithViews();

// Registrar servicios de la aplicación
builder.Services.AddScoped<IVisitasService, VisitasService>();
builder.Services.AddScoped<IReservasService, ReservasService>();

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

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