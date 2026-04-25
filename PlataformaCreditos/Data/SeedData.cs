using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using System.Security.Claims;

namespace PlataformaCreditos.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Verificar si ya hay datos
            if (context.Clientes.Any() || context.SolicitudesCredito.Any())
            {
                return;
            }

            // Crear rol de Analista si no existe
            if (!await roleManager.RoleExistsAsync("Analista"))
            {
                await roleManager.CreateAsync(new IdentityRole("Analista"));
            }

            // Crear usuario Analista
            var analistaEmail = "analista@plataforma.com";
            var analistaUser = await userManager.FindByEmailAsync(analistaEmail);
            
            if (analistaUser == null)
            {
                analistaUser = new IdentityUser
                {
                    UserName = analistaEmail,
                    Email = analistaEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(analistaUser, "Analista123!");
                await userManager.AddToRoleAsync(analistaUser, "Analista");
            }

            // Crear 2 clientes de prueba
            var cliente1Email = "cliente1@test.com";
            var cliente2Email = "cliente2@test.com";

            var cliente1User = await userManager.FindByEmailAsync(cliente1Email);
            if (cliente1User == null)
            {
                cliente1User = new IdentityUser
                {
                    UserName = cliente1Email,
                    Email = cliente1Email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(cliente1User, "Cliente123!");
            }

            var cliente2User = await userManager.FindByEmailAsync(cliente2Email);
            if (cliente2User == null)
            {
                cliente2User = new IdentityUser
                {
                    UserName = cliente2Email,
                    Email = cliente2Email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(cliente2User, "Cliente123!");
            }

            // Crear clientes en la base de datos
            var cliente1 = new Cliente
            {
                UsuarioId = cliente1User.Id,
                IngresosMensuales = 5000m,
                Activo = true
            };

            var cliente2 = new Cliente
            {
                UsuarioId = cliente2User.Id,
                IngresosMensuales = 8000m,
                Activo = true
            };

            context.Clientes.AddRange(cliente1, cliente2);
            await context.SaveChangesAsync();

            // Crear solicitudes
            var solicitud1 = new SolicitudCredito
            {
                ClienteId = cliente1.Id,
                MontoSolicitado = 15000m, // 3x ingresos = válido
                FechaSolicitud = DateTime.Now.AddDays(-2),
                Estado = EstadoSolicitud.Pendiente
            };

            var solicitud2 = new SolicitudCredito
            {
                ClienteId = cliente2.Id,
                MontoSolicitado = 20000m, // 2.5x ingresos = válido
                FechaSolicitud = DateTime.Now.AddDays(-5),
                Estado = EstadoSolicitud.Aprobado
            };

            context.SolicitudesCredito.AddRange(solicitud1, solicitud2);
            await context.SaveChangesAsync();

            Console.WriteLine("Seed data creado exitosamente:");
            Console.WriteLine($"  - Usuario Analista: {analistaEmail} (password: Analista123!)");
            Console.WriteLine($"  - Cliente 1: {cliente1Email} (password: Cliente123!)");
            Console.WriteLine($"  - Cliente 2: {cliente2Email} (password: Cliente123!)");
            Console.WriteLine($"  - Solicitud 1: Pendiente (Monto: {solicitud1.MontoSolicitado})");
            Console.WriteLine($"  - Solicitud 2: Aprobada (Monto: {solicitud2.MontoSolicitado})");
        }
    }
}