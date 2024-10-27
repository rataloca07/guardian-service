using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using GuardianService.Services;
using GuardianService.Models;
using Newtonsoft.Json;

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
        /*[HttpPost("registrar")]
        public async Task<IActionResult> RegistrarPaciente([FromBody] Paciente paciente)
        {
            if (paciente == null)
            {
                return BadRequest(new { message = "Datos del paciente vac�os" });
            }

            if (string.IsNullOrEmpty(paciente.Nombre))
            {
                return BadRequest(new { message = "Nombre obligatorio" });
            }

            if (string.IsNullOrEmpty(paciente.SIM))
            {
                return BadRequest(new { message = "Sim obligatoria" });
            }

            if (string.IsNullOrEmpty(paciente.GuardianId))
            {
                return BadRequest(new { message = "No tiene guardi�n asociado" });
            }

            await _firestoreService.RegistrarPaciente(paciente);
            return Ok(new { message = "Paciente registrado correctamente" });
        }*/

        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarPaciente([FromBody] Paciente paciente)
        {
            if (paciente == null)
            {
                return BadRequest(new { message = "Datos del paciente vac�os" });
            }

            if (string.IsNullOrEmpty(paciente.Nombre))
            {
                return BadRequest(new { message = "Nombre obligatorio" });
            }

            if (string.IsNullOrEmpty(paciente.SIM))
            {
                return BadRequest(new { message = "Sim obligatoria" });
            }

            if (string.IsNullOrEmpty(paciente.GuardianId))
            {
                return BadRequest(new { message = "No tiene guardi�n asociado" });
            }

            // Llamamos al servicio para registrar el paciente y obtenemos el paciente registrado con su ID
            var pacienteRegistrado = await _firestoreService.RegistrarPaciente(paciente);

            // Retornar el ID del paciente registrado para que el frontend lo maneje adecuadamente
            return Ok(new { PacienteId = pacienteRegistrado.Id, message = "Paciente registrado correctamente" });
        }

        // Actualizar ubicaci�n y ritmo card�aco desde el dispositivo IoT
        [HttpPost("actualizar")]
        public async Task<IActionResult> ActualizarEstadoPaciente([FromBody] ActualizarEstadoModel model)
        {

            // Recuperamos al paciente usando el n�mero de SIM
            var paciente = await _firestoreService.ObtenerPacientePorSIM(model.SIM);
            if (paciente == null)
                return NotFound(new { message = "Paciente no encontrado" });


            //Verificar si est� en zona segura en sus coordenadas anteriores para no repetir notificaci�n
            var coordOldFueraDeZonaSegura = await _firestoreService.PacienteFueraDeZonaSegura(paciente.Id, paciente.Latitud, paciente.Longitud);

            // Actualizamos el estado del paciente (latitud, longitud, ritmo card�aco)
            await _firestoreService.ActualizarEstadoPaciente(model.SIM, model.Latitud, model.Longitud, model.RitmoCardiaco);

            // Si est� fuera de la zona segura, enviamos la notificaci�n al guardi�n
            if (coordOldFueraDeZonaSegura)
            {
                return Ok(new { message = "Paciente sigue fuera de zona segura. No repetir notificaci�n" });
            }

            // Verificamos si el paciente est� fuera de la zona segura
            var estaFueraDeZonaSegura = await _firestoreService.PacienteFueraDeZonaSegura(paciente.Id, model.Latitud, model.Longitud);

            // Si est� fuera de la zona segura, enviamos la notificaci�n al guardi�n
            if (estaFueraDeZonaSegura)
            {
                Console.WriteLine("---------------Paciente salio de zona segura----------------");
                Console.WriteLine("Paciente: ");
                Console.WriteLine(JsonConvert.SerializeObject(paciente));
                var guardian = await _firestoreService.ObtenerGuardianPorId(paciente.GuardianId);
                Console.WriteLine("guardian: ");
                Console.WriteLine(JsonConvert.SerializeObject(guardian));
                Console.WriteLine("---------------Intenta obtener guardian----------------");
                if (guardian != null && !string.IsNullOrEmpty(guardian.TokenDispositivo))
                {
                    Console.WriteLine("---------Obtuvo guardian----------------");
                    await _firestoreService.EnviarNotificacionAlerta(
                        guardian.TokenDispositivo,
                        "Alerta: Paciente fuera de la zona segura",
                        $"El paciente asociado al guardi�n {guardian.Nombre} ha salido de la zona segura.");
                }

                return Ok(new { message = "Paciente fuera de la zona segura, alerta enviada" });
            }
            else
            {
                Console.WriteLine("---------------Paciente sigue dentro de zona segura----------------");
            }

            return Ok(new { message = "Paciente dentro de la zona segura, sin alerta." });
        }

        [HttpPost("actualizarIoT")]
        [AllowAnonymous]  // Este m�todo es accesible sin autenticaci�n
        public async Task<IActionResult> ActualizarEstadoPacienteIoT([FromBody] ActualizarEstadoModel model)
        {
            Console.WriteLine("---------------ActualizarEstadoPacienteIoT----------------");
            // Verificar si el n�mero SIM asociado al paciente existe
            var paciente = await _firestoreService.ObtenerPacientePorSIM(model.SIM);
            Console.WriteLine("------Busca paciente por SIM---");
            if (paciente == null)
            {
                Console.WriteLine("------No encontr� paciente---");
                return NotFound(new { message = "Paciente no encontrado" });
            }
            Console.WriteLine("------Encontr� paciente---");
            //Verificar si est� en zona segura en sus coordenadas anteriores para no repetir notificaci�n
            var coordOldFueraDeZonaSegura = await _firestoreService.PacienteFueraDeZonaSegura(paciente.Id, paciente.Latitud, paciente.Longitud);

            // Actualizar la ubicaci�n y el ritmo card�aco del paciente
            await _firestoreService.ActualizarEstadoPaciente(model.SIM, model.Latitud, model.Longitud, model.RitmoCardiaco);
            // Si est� fuera de la zona segura, enviamos la notificaci�n al guardi�n
            if (coordOldFueraDeZonaSegura)
            {
                Console.WriteLine("------Coordenada anterior fuera de zona segura---");
                return Ok(new { message = "Paciente sigue fuera de zona segura. No repetir notificaci�n" });
            }

            // Verificar si el paciente est� fuera de la zona segura
            var estaFueraDeZonaSegura = await _firestoreService.PacienteFueraDeZonaSegura(paciente.Id, model.Latitud, model.Longitud);

            // Si est� fuera de la zona segura, enviamos la notificaci�n al guardi�n
            if (estaFueraDeZonaSegura)
            {
                Console.WriteLine("------Ubicaci�n actual fuera de zona segura---");
                var guardian = await _firestoreService.ObtenerGuardianPorId(paciente.GuardianId);
                if (guardian != null && !string.IsNullOrEmpty(guardian.TokenDispositivo))
                {
                    await _firestoreService.EnviarNotificacionAlerta(
                        guardian.TokenDispositivo,
                        "Alerta: Paciente fuera de la zona segura",
                        $"El paciente asociado al guardi�n {guardian.Nombre} ha salido de la zona segura."
                    );
                }

                return Ok(new { message = "Paciente fuera de la zona segura, alerta enviada" });
            }
            Console.WriteLine("------Ubicaci�n actual dentro de zona segura---");

            return Ok(new { message = "Paciente dentro de la zona segura, sin alerta." });
        }


        [HttpGet("obtener/{pacienteId}")]
        public async Task<IActionResult> ObtenerPaciente(string pacienteId)
        {
            var paciente = await _firestoreService.ObtenerPacientePorId(pacienteId);
            if (paciente == null) return NotFound(new { message = "Paciente no encontrado" });

            return Ok(paciente);
        }

        // M�todo para modificar un paciente
        [HttpPut("modificar")]
        public async Task<IActionResult> ModificarPaciente([FromBody] Paciente paciente)
        {
            if (paciente == null || string.IsNullOrEmpty(paciente.Id))
            {
                return BadRequest(new { message = "Datos del paciente inv�lidos." });
            }

            // Llamamos al servicio para modificar el paciente
            bool result = await _firestoreService.ModificarPaciente(paciente);

            if (result)
            {
                return Ok(new { message = "Paciente modificado exitosamente." });
            }

            return StatusCode(500, new { message = "Hubo un error al modificar el paciente." });
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