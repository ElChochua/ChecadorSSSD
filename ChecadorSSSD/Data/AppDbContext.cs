using Microsoft.EntityFrameworkCore;
using ChecadorSSSD.Models;

namespace ChecadorSSSD.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Personas> Personas { get; set; }
    public DbSet<Checadores> Checadores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Convertir enums a strings en la base de datos
        modelBuilder.Entity<Personas>()
            .Property(p => p.TipoPersona)
            .HasConversion<string>();

        modelBuilder.Entity<Checadores>()
            .Property(c => c.TipoAccion)
            .HasConversion<string>();

        // Índice único para Matricula
        modelBuilder.Entity<Personas>()
            .HasIndex(p => p.Matricula)
            .IsUnique();

        // Relación: una Persona tiene muchos Checadores
        modelBuilder.Entity<Personas>()
            .HasMany(p => p.Registros)
            .WithOne(c => c.Persona)
            .HasForeignKey(c => c.PersonaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
