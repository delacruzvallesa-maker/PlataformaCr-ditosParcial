using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<SolicitudCredito> SolicitudesCredito { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configuración Cliente
        builder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IngresosMensuales).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            
            // Un cliente solo puede tener un usuario
            entity.HasIndex(e => e.UsuarioId).IsUnique();
        });

        // Configuración SolicitudCredito
        builder.Entity<SolicitudCredito>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MontoSolicitado).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.FechaSolicitud).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.Estado).HasDefaultValue(EstadoSolicitud.Pendiente);
            
            // Relación con Cliente
            entity.HasOne(e => e.Cliente)
                .WithMany(c => c.Solicitudes)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Índice para buscar solicitudes por cliente y estado
            entity.HasIndex(e => new { e.ClienteId, e.Estado });
        });
    }

    public override int SaveChanges()
    {
        // Validaciones de negocio antes de guardar
        ValidarEntidades();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidarEntidades();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidarEntidades()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // Validar Cliente
            if (entry.Entity is Cliente cliente)
            {
                if (cliente.IngresosMensuales <= 0)
                {
                    throw new InvalidOperationException("Los ingresos mensuales deben ser mayores a 0.");
                }
            }

            // Validar SolicitudCredito
            if (entry.Entity is SolicitudCredito solicitud)
            {
                if (solicitud.MontoSolicitado <= 0)
                {
                    throw new InvalidOperationException("El monto solicitado debe ser mayor a 0.");
                }

                // Validar que no exista otra solicitud pendiente para el mismo cliente
                if (solicitud.Estado == EstadoSolicitud.Pendiente)
                {
                    var clienteId = solicitud.ClienteId;
                    var solicitudesPendientes = SolicitudesCredito
                        .Any(s => s.ClienteId == clienteId && s.Estado == EstadoSolicitud.Pendiente && s.Id != solicitud.Id);
                    
                    if (solicitudesPendientes)
                    {
                        throw new InvalidOperationException("El cliente ya tiene una solicitud pendiente.");
                    }
                }
            }
        }
    }
}
