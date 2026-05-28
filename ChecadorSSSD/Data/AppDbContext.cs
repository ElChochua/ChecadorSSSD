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
    public DbSet<AsistenciaChecador> AsistenciasChecador { get; set; }
    public DbSet<Administrador> Administradores { get; set; }

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

        // Configuracion para Administradores
        modelBuilder.Entity<Administrador>().ToTable("administradores");
        modelBuilder.Entity<Administrador>()
            .Property(a => a.IdAdmin)
            .HasColumnName("id_admin");
        modelBuilder.Entity<Administrador>()
            .Property(a => a.IdPersona)
            .HasColumnName("id_persona");
        modelBuilder.Entity<Administrador>()
            .Property(a => a.Usuario)
            .HasColumnName("usuario");
        modelBuilder.Entity<Administrador>()
            .Property(a => a.Contrasenia)
            .HasColumnName("contrasenia");

        // Configuracion de tabla y columnas para Personas
        modelBuilder.Entity<Personas>().ToTable("personas");
        modelBuilder.Entity<Personas>()
            .Property(p => p.IdPersona)
            .HasColumnName("id_persona");
        modelBuilder.Entity<Personas>()
            .Property(p => p.Nombre)
            .HasColumnName("nombre");
        modelBuilder.Entity<Personas>()
            .Property(p => p.ApellidoPaterno)
            .HasColumnName("apellido_paterno");
        modelBuilder.Entity<Personas>()
            .Property(p => p.ApellidoMaterno)
            .HasColumnName("apellido_materno");
        modelBuilder.Entity<Personas>()
            .Property(p => p.TipoPersona)
            .HasColumnName("tipo_persona");
        modelBuilder.Entity<Personas>()
            .Property(p => p.Matricula)
            .HasColumnName("matricula");
        modelBuilder.Entity<Personas>()
            .Property(p => p.Huella)
            .HasColumnName("huella");
        modelBuilder.Entity<Personas>()
            .Property(p => p.Imagen)
            .HasColumnName("imagen");

        // Indice unico para Matricula
        modelBuilder.Entity<Personas>()
            .HasIndex(p => p.Matricula)
            .IsUnique();

        // Configuracion para AsistenciasChecador
        modelBuilder.Entity<AsistenciaChecador>().ToTable("asistencias_checador");
        modelBuilder.Entity<AsistenciaChecador>()
            .Property(a => a.IdAsistencia)
            .HasColumnName("id_asistencia");
        modelBuilder.Entity<AsistenciaChecador>()
            .Property(a => a.IdPersona)
            .HasColumnName("id_persona");
        modelBuilder.Entity<AsistenciaChecador>()
            .Property(a => a.HoraEntrada)
            .HasColumnName("hora_entrada");
        modelBuilder.Entity<AsistenciaChecador>()
            .Property(a => a.HoraSalida)
            .HasColumnName("hora_salida");
        modelBuilder.Entity<AsistenciaChecador>()
            .Property(a => a.Estatus)
            .HasColumnName("estatus");
    }
}
