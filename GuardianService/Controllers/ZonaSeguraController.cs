using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using GuardianService.Services;
using GuardianService.Models;


namespace GuardianService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ZonaSeguraController : ControllerBase
    {
        private readonly FirestoreService _firestoreService;

        public ZonaSeguraController(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        // Registrar una nueva zona segura
        /*[HttpPost("registrar")]
        public async Task<IActionResult> RegistrarZonaSegura([FromBody] ZonaSegura zonaSegura)
        {
            await _firestoreService.RegistrarZonaSegura(zonaSegura);
            return Ok(new { message = "Zona segura registrada correctamente" });
        }*/
        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarZonaSegura([FromBody] ZonaSegura zonaSegura)
        {
            if (zonaSegura == null)
            {
                return BadRequest("Datos inválidos.");
            }

            // Registrar la zona segura en Firestore
            try
            {
                await _firestoreService.RegistrarZonaSegura(zonaSegura);
                return Ok(new { message = "Zona segura registrada correctamente." });
            }
            catch (Exception ex)
            {
                // Aquí se captura el error y se puede hacer logging del detalle
                return StatusCode(500, $"Error al registrar la zona segura: {ex.Message}");
            }
        }

        // Verificar si el paciente está fuera de la zona segura
        [HttpGet("verificar/{pacienteId}")]
        public async Task<IActionResult> VerificarPacienteFueraDeZona(string pacienteId, [FromQuery] double latitud, [FromQuery] double longitud)
        {
            var fueraDeZona = await _firestoreService.PacienteFueraDeZonaSegura(pacienteId, latitud, longitud);
            if (fueraDeZona)
            {
                // Obtener el token del dispositivo del guardián
                var paciente = await _firestoreService.ObtenerPacientePorId(pacienteId);
                var guardian = await _firestoreService.ObtenerGuardianPorId(paciente.GuardianId);

                // Enviar la notificación al guardián
                await _firestoreService.EnviarNotificacionAlerta(guardian.TokenDispositivo, "Alerta: Paciente fuera de la zona segura", $"El paciente {pacienteId} ha salido de la zona segura.");

                return Ok(new { message = "Paciente fuera de la zona segura, alerta enviada" });
            }

            return Ok(new { message = "Paciente dentro de la zona segura" });
        }

        // PUT: /api/zonasegura/modificar
        [HttpPut("modificar")]
        public async Task<IActionResult> ModificarZonaSegura([FromBody] ZonaSegura zonaSegura)
        {
            await _firestoreService.ModificarZonaSegura(zonaSegura);
            return Ok(new { message = "Zona segura modificada correctamente" });
        }

        // DELETE: /api/zonasegura/eliminar/{zonaId}
        [HttpDelete("eliminar/{zonaId}")]
        public async Task<IActionResult> EliminarZonaSegura(string zonaId)
        {
            await _firestoreService.EliminarZonaSegura(zonaId);
            return Ok(new { message = "Zona segura eliminada correctamente" });
        }


        [HttpGet("paciente/{pacienteId}")]
        public async Task<IActionResult> ObtenerZonasSegurasPorPaciente(string pacienteId)
        {
            var zonas = await _firestoreService.ObtenerZonasSegurasPorPaciente(pacienteId);
            return Ok(zonas);
        }
    }
}