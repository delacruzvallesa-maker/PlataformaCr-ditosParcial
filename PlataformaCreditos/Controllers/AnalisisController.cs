using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Controllers
{
    [Authorize]
    public class AnalisisController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnalisisController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Analisis/Index - Lista todas las solicitudes
        public async Task<IActionResult> Index(string? estado = null)
        {
            IQueryable<SolicitudCredito> query = _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .ThenInclude(c => c!.Usuario)
                .OrderByDescending(s => s.FechaSolicitud);

            if (!string.IsNullOrEmpty(estado) && Enum.TryParse<EstadoSolicitud>(estado, out var estadoEnum))
            {
                query = query.Where(s => s.Estado == estadoEnum);
            }

            var solicitudes = await query.ToListAsync();
            ViewData["EstadoFilter"] = estado;
            return View(solicitudes);
        }

        // GET: /Analisis/Detalles/5
        public async Task<IActionResult> Detalles(int id)
        {
            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .ThenInclude(c => c!.Usuario)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            return View(solicitud);
        }

        // POST: /Analisis/Aprobar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["Error"] = "Solo se pueden aprobar solicitudes pendientes.";
                return RedirectToAction(nameof(Index));
            }

            // Validar regla de negocio: MontoSolicitado <= 5 * IngresosMensuales
            if (solicitud.Cliente != null && solicitud.MontoSolicitado > (solicitud.Cliente.IngresosMensuales * 5))
            {
                TempData["Error"] = "No se puede aprobar: el monto solicitado excede 5 veces los ingresos mensuales del cliente.";
                return RedirectToAction(nameof(Detalles), new { id });
            }

            solicitud.Estado = EstadoSolicitud.Aprobado;
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Solicitud #{solicitud.Id} aprobada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Analisis/Rechazar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id, string motivo)
        {
            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["Error"] = "Solo se pueden rechazar solicitudes pendientes.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                ModelState.AddModelError("", "Debe proporcionar un motivo de rechazo.");
                return RedirectToAction(nameof(Detalles), new { id });
            }

            solicitud.Estado = EstadoSolicitud.Rechazado;
            solicitud.MotivoRechazo = motivo;
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Solicitud #{solicitud.Id} rechazada.";
            return RedirectToAction(nameof(Index));
        }
    }
}