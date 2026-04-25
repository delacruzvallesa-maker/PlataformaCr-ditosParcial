using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Controllers
{
    [Authorize]
    public class SolicitudController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SolicitudController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Solicitud/MisSolicitudes
        public async Task<IActionResult> MisSolicitudes()
        {
            var usuarioId = User.Identity?.Name;
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cliente = await _context.Clientes
                .Include(c => c.Solicitudes)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (cliente == null)
            {
                // El usuario no es cliente, redirigir a crear perfil
                return RedirectToAction("CrearPerfil");
            }

            return View(cliente.Solicitudes?.ToList() ?? new List<SolicitudCredito>());
        }

        // GET: /Solicitud/CrearPerfil
        public IActionResult CrearPerfil()
        {
            return View();
        }

        // POST: /Solicitud/CrearPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPerfil([Bind("IngresosMensuales")] Cliente clienteViewModel)
        {
            var usuarioId = User.Identity?.Name;
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Verificar si ya existe un cliente para este usuario
            var clienteExistente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (clienteExistente != null)
            {
                ModelState.AddModelError("", "Ya tienes un perfil de cliente registrado.");
                return View(clienteViewModel);
            }

            if (ModelState.IsValid)
            {
                var nuevoCliente = new Cliente
                {
                    UsuarioId = usuarioId,
                    IngresosMensuales = clienteViewModel.IngresosMensuales,
                    Activo = true
                };

                _context.Clientes.Add(nuevoCliente);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Perfil creado correctamente. Ahora puedes registrar solicitudes de crédito.";
                return RedirectToAction(nameof(MisSolicitudes));
            }

            return View(clienteViewModel);
        }

        // GET: /Solicitud/Crear
        public async Task<IActionResult> Crear()
        {
            var usuarioId = User.Identity?.Name;
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (cliente == null)
            {
                return RedirectToAction("CrearPerfil");
            }

            // Verificar si ya tiene una solicitud pendiente
            var tienePendiente = await _context.SolicitudesCredito
                .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);

            if (tienePendiente)
            {
                TempData["Error"] = "Ya tienes una solicitud pendiente. No puedes crear otra hasta que sea procesada.";
                return RedirectToAction(nameof(MisSolicitudes));
            }

            ViewData["ClienteId"] = cliente.Id;
            ViewData["IngresosMensuales"] = cliente.IngresosMensuales;
            ViewData["MontoMaximo"] = cliente.IngresosMensuales * 5;

            return View();
        }

        // POST: /Solicitud/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear([Bind("MontoSolicitado")] SolicitudCredito solicitud)
        {
            var usuarioId = User.Identity?.Name;
            if (string.IsNullOrEmpty(usuarioId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            if (cliente == null)
            {
                return RedirectToAction("CrearPerfil");
            }

            // Verificar si ya tiene una solicitud pendiente
            var tienePendiente = await _context.SolicitudesCredito
                .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);

            if (tienePendiente)
            {
                ModelState.AddModelError("", "Ya tienes una solicitud pendiente.");
                ViewData["ClienteId"] = cliente.Id;
                ViewData["IngresosMensuales"] = cliente.IngresosMensuales;
                ViewData["MontoMaximo"] = cliente.IngresosMensuales * 5;
                return View(solicitud);
            }

            if (ModelState.IsValid)
            {
                solicitud.ClienteId = cliente.Id;
                solicitud.FechaSolicitud = DateTime.Now;
                solicitud.Estado = EstadoSolicitud.Pendiente;

                _context.SolicitudesCredito.Add(solicitud);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Solicitud de crédito registrada correctamente.";
                return RedirectToAction(nameof(MisSolicitudes));
            }

            ViewData["ClienteId"] = cliente.Id;
            ViewData["IngresosMensuales"] = cliente.IngresosMensuales;
            ViewData["MontoMaximo"] = cliente.IngresosMensuales * 5;
            return View(solicitud);
        }
    }
}