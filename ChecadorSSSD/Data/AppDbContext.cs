using Microsoft.EntityFrameworkCore;
using ChecadorSSSD.Models;

namespace ChecadorSSSD.Data;

public class AppDbContext : DbContext
{
    private readonly string _connectionString;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        _connectionString = string.Empty;
    }

    public AppDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<Personas> Personas { get; set; }
    public DbSet<Checadores> Checadores { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_connectionString))
        {
            optionsBuilder.UseMySql(
                _connectionString,
                ServerVersion.AutoDetect(_connectionString),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure()
            );
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuraci n de tabla y columnas para Personas
        modelBuilder.Entity<Personas>().ToTable("personas");
        modelBuilder.Entity<Personas>()
            .Property(p => p.IdPersonas)
            .HasColumnName("id_personas");
        modelBuilder.Entity<Personas>()
            .Property(p => p.ApellidoPaterno)
            .HasColumnName("Apellido_paterno");
        modelBuilder.Entity<Personas>()
            .Property(p => p.ApellidoMaterno)
            .HasColumnName("Apellido_materno");
        modelBuilder.Entity<Personas>()
            .Property(p => p.TipoPersona)
            .HasColumnName("Tipo_persona")
            .IsRequired(false);
        modelBuilder.Entity<Personas>()
            .Property(p => p.Huella)
            .HasColumnName("Huella")
            .HasColumnType("LONGBLOB");
        modelBuilder.Entity<Personas>()
            .Property(p => p.Imagen)
            .HasColumnName("Imagen");
        modelBuilder.Entity<Personas>()
            .Property(p => p.Matricula)
            .HasColumnName("Matricula");
        modelBuilder.Entity<Personas>()
            .Property(p => p.Nombre)
            .HasColumnName("Nombre");

        //  ndice  nico para Matricula
        modelBuilder.Entity<Personas>()
            .HasIndex(p => p.Matricula)
            .IsUnique();

        // Configuraci n para Checadores (sin ValueConverters - mapeo directo)
        modelBuilder.Entity<Checadores>().ToTable("checador");
        modelBuilder.Entity<Checadores>()
            .Property(c => c.IdChecador)
            .HasColumnName("id_checador");
        modelBuilder.Entity<Checadores>()
            .Property(c => c.TipoAccion)
            .HasColumnName("Tipo_accion")
            .IsRequired(false);
        modelBuilder.Entity<Checadores>()
            .Property(c => c.Fecha)
            .HasColumnName("Fecha");
        modelBuilder.Entity<Checadores>()
            .Property(c => c.HoraRaw)
            .HasColumnName("Hora")
            .IsRequired(false);
        modelBuilder.Entity<Checadores>()
            .Property(c => c.Matricula)
            .HasColumnName("Matricula");
        modelBuilder.Entity<Checadores>()
            .Property(c => c.Nombre)
            .HasColumnName("Nombre");
        modelBuilder.Entity<Checadores>()
            .Property(c => c.ApellidoPaterno)
            .HasColumnName("Apellido_paterno");
        modelBuilder.Entity<Checadores>()
            .Property(c => c.ApellidoMaterno)
            .HasColumnName("Apellido_materno");
        modelBuilder.Entity<Checadores>()
            .Property(c => c.TipoPersona)
            .HasColumnName("Tipo_persona")
            .IsRequired(false);
    }
}
