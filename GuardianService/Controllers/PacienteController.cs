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
    public class PacienteController : ControllerBase
    {
        private readonly FirestoreService _firestoreService;

        public PacienteController(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        // Registrar un paciente
        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarPaciente([FromBody] Paciente paciente)
        {
            await _firestoreService.RegistrarPaciente(paciente);
            return Ok(new { message = "Paciente registrado correctamente" });
        }

        // Actualizar ubicación y ritmo cardíaco desde el dispositivo IoT
        [HttpPost("actualizar")]
        public async Task<IActionResult> ActualizarEstadoPaciente([FromBody] ActualizarEstadoModel model)
        {
            // Actualizamos el estado del paciente (latitud, longitud, ritmo cardíaco)
            await _firestoreService.ActualizarEstadoPaciente(model.SIM, model.Latitud, model.Longitud, model.RitmoCardiaco);

            // Recuperamos al paciente usando el número de SIM
            var paciente = await _firestoreService.ObtenerPacientePorSIM(model.SIM);
            if (paciente == null)
                return NotFound(new { message = "Paciente no encontrado" });

            // Verificamos si el paciente está fuera de la zona segura
            var estaFueraDeZonaSegura = await _firestoreService.PacienteFueraDeZonaSegura(paciente.Id, model.Latitud, model.Longitud);

            // Si está fuera de la zona segura, enviamos la notificación al guardián
            if (estaFueraDeZonaSegura)
            {
                Console.WriteLine("---------------Paciente salio de zona segura----------------");
                var guardian = await _firestoreService.ObtenerGuardianPorId(paciente.GuardianId);
                if (guardian != null && !string.IsNullOrEmpty(guardian.TokenDispositivo))
                {
                    await _firestoreService.EnviarNotificacionAlerta(
                        guardian.TokenDispositivo,
                        "Alerta: Paciente fuera de la zona segura",
                        $"El paciente asociado al guardián {guardian.Nombre} ha salido de la zona segura.");
                }

                return Ok(new { message = "Paciente fuera de la zona segura, alerta enviada" });
            }
            else
            {
                Console.WriteLine("---------------Paciente sigue dentro de zona segura----------------");
            }

            return Ok(new { message = "Paciente dentro de la zona segura, sin alerta." });
        }


        [HttpGet("obtener/{pacienteId}")]
        public async Task<IActionResult> ObtenerPaciente(string pacienteId)
        {
            var paciente = await _firestoreService.ObtenerPacientePorId(pacienteId);
            if (paciente == null) return NotFound(new { message = "Paciente no encontrado" });

            return Ok(paciente);
        }
    }

    public class ActualizarEstadoModel
    {
        public string SIM { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public int RitmoCardiaco { get; set; }
    }
}