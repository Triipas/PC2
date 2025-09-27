using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // Asegurar que la base de datos está creada
            await context.Database.EnsureCreatedAsync();

            // Si ya hay inmuebles, no insertar datos de prueba
            if (await context.Inmuebles.AnyAsync())
            {
                return;
            }

            var inmuebles = new[]
            {
                new Inmueble
                {
                    Codigo = "DEPT001",
                    Titulo = "Moderno Departamento en San Isidro",
                    Imagen = "https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?w=500",
                    Tipo = TipoInmueble.Departamento,
                    Ciudad = "Lima",
                    Direccion = "Av. Javier Prado 1234, San Isidro",
                    Dormitorios = 3,
                    Banos = 2,
                    MetrosCuadrados = 120.5m,
                    Precio = 450000m,
                    Activo = true
                },
                new Inmueble
                {
                    Codigo = "CASA001",
                    Titulo = "Casa Familiar en La Molina",
                    Imagen = "https://images.unsplash.com/photo-1564013799919-ab600027ffc6?w=500",
                    Tipo = TipoInmueble.Casa,
                    Ciudad = "Lima",
                    Direccion = "Calle Los Eucaliptos 567, La Molina",
                    Dormitorios = 4,
                    Banos = 3,
                    MetrosCuadrados = 280.0m,
                    Precio = 750000m,
                    Activo = true
                },
                new Inmueble
                {
                    Codigo = "OFIC001",
                    Titulo = "Oficina Corporativa Centro Financiero",
                    Imagen = "https://images.unsplash.com/photo-1497366216548-37526070297c?w=500",
                    Tipo = TipoInmueble.Oficina,
                    Ciudad = "Lima",
                    Direccion = "Av. República de Panamá 890, San Isidro",
                    Dormitorios = 0,
                    Banos = 2,
                    MetrosCuadrados = 85.5m,
                    Precio = 320000m,
                    Activo = true
                },
                new Inmueble
                {
                    Codigo = "LOCAL001",
                    Titulo = "Local Comercial en Miraflores",
                    Imagen = "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=500",
                    Tipo = TipoInmueble.Local,
                    Ciudad = "Lima",
                    Direccion = "Av. Larco 445, Miraflores",
                    Dormitorios = 0,
                    Banos = 1,
                    MetrosCuadrados = 65.0m,
                    Precio = 280000m,
                    Activo = true
                },
                new Inmueble
                {
                    Codigo = "DEPT002",
                    Titulo = "Departamento Vista al Mar - Barranco",
                    Imagen = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=500",
                    Tipo = TipoInmueble.Departamento,
                    Ciudad = "Lima",
                    Direccion = "Malecón Paul Harris 789, Barranco",
                    Dormitorios = 2,
                    Banos = 2,
                    MetrosCuadrados = 95.0m,
                    Precio = 380000m,
                    Activo = true
                },
                new Inmueble
                {
                    Codigo = "CASA002",
                    Titulo = "Casa de Campo - Cieneguilla",
                    Imagen = "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=500",
                    Tipo = TipoInmueble.Casa,
                    Ciudad = "Lima",
                    Direccion = "Km 25 Carretera Central, Cieneguilla",
                    Dormitorios = 5,
                    Banos = 4,
                    MetrosCuadrados = 450.0m,
                    Precio = 650000m,
                    Activo = false // Una propiedad inactiva para testing
                }
            };

            context.Inmuebles.AddRange(inmuebles);
            await context.SaveChangesAsync();

            Console.WriteLine("✅ Datos de seed insertados correctamente:");
            Console.WriteLine($"   - {inmuebles.Count(i => i.Activo)} inmuebles activos");
            Console.WriteLine($"   - {inmuebles.Count(i => !i.Activo)} inmuebles inactivos");
            Console.WriteLine($"   - Tipos: {inmuebles.GroupBy(i => i.Tipo).Count()} diferentes");
        }
    }
}