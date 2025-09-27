using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Inmueble> Inmuebles { get; set; }
        public DbSet<Visita> Visitas { get; set; }
        public DbSet<Reserva> Reservas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración para Inmueble
            modelBuilder.Entity<Inmueble>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Codigo).IsUnique();
                
                entity.Property(e => e.Precio)
                    .HasPrecision(18, 2);
                
                entity.Property(e => e.MetrosCuadrados)
                    .HasPrecision(18, 2);

                // Relaciones
                entity.HasMany(e => e.Visitas)
                    .WithOne(e => e.Inmueble)
                    .HasForeignKey(e => e.InmuebleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Reservas)
                    .WithOne(e => e.Inmueble)
                    .HasForeignKey(e => e.InmuebleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración para Visita
            modelBuilder.Entity<Visita>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Usuario)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.InmuebleId, e.FechaInicio, e.FechaFin })
                    .HasDatabaseName("IX_Visitas_Inmueble_Fechas");
            });

            // Configuración para Reserva
            modelBuilder.Entity<Reserva>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Usuario)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.InmuebleId, e.FechaExpiracion })
                    .HasDatabaseName("IX_Reservas_Inmueble_Expiracion");
            });

            // Conversión de enums
            modelBuilder.Entity<Inmueble>()
                .Property(e => e.Tipo)
                .HasConversion<int>();

            modelBuilder.Entity<Visita>()
                .Property(e => e.Estado)
                .HasConversion<int>();

            // Constraints de verificación (check constraints)
            modelBuilder.Entity<Inmueble>()
                .ToTable(t => t.HasCheckConstraint("CK_Inmueble_Precio_Positive", "[Precio] > 0"));

            modelBuilder.Entity<Inmueble>()
                .ToTable(t => t.HasCheckConstraint("CK_Inmueble_MetrosCuadrados_Positive", "[MetrosCuadrados] > 0"));
        }
    }
}