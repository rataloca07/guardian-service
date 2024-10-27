using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using GuardianService.Services;
using GuardianService.Models;


namespace GuardianService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GuardianController : ControllerBase
    {
        private readonly FirestoreService _firestoreService;
        private readonly IAuthService _authService;

        public GuardianController(FirestoreService firestoreService, IAuthService authService)
        {
            _firestoreService = firestoreService;
            _authService = authService;
        }

        // Registrar un nuevo guardián
        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarGuardian([FromBody] Guardian guardian)
        {
            if (guardian == null || string.IsNullOrEmpty(guardian.Email) || string.IsNullOrEmpty(guardian.Password) || string.IsNullOrEmpty(guardian.Nombre))
            {
                return BadRequest(new { message = "Todos los campos son obligatorios." });
            }

            guardian.Email = guardian.Email.Trim().ToLower();

            var guardianExiste = await _firestoreService.ObtenerGuardianPorEmail(guardian.Email);
            if (guardianExiste != null)
            {
                return BadRequest(new { message = "El correo ya se encuentra registrado. " });
            }

            // Cifrar la contraseña antes de guardar
            guardian.Password = BCrypt.Net.BCrypt.HashPassword(guardian.Password);
            await _firestoreService.RegistrarGuardian(guardian);
            return Ok(new { message = "Guardián registrado correctamente" });
        }

        // Iniciar sesión y generar JWT
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new { message = "Todos los campos son obligatorios." });
            }

            model.Email = model.Email.Trim().ToLower();

            var guardian = await _firestoreService.ObtenerGuardianPorEmail(model.Email);
            if (guardian == null || !BCrypt.Net.BCrypt.Verify(model.Password, guardian.Password))
                return Unauthorized(new { message = "Credenciales incorrectas" });

            var token = _authService.GenerateToken(guardian.Id);

            // Obtener el ID del paciente asociado al guardián
            var paciente = await _firestoreService.ObtenerPacientePorGuardianId(guardian.Id);
            //return Ok(new { Token = token });
            return Ok(new
            {
                Token = token,
                GuardianId = guardian.Id,           // Enviar el ID del guardián
                PacienteId = paciente?.Id,          // Enviar el ID del paciente si existe
                GuardianNombre = guardian.Nombre    // Enviar el nombre del guardián
            });
        }

        [HttpPut("actualizarTokenDispositivo")]
        public async Task<IActionResult> ActualizarTokenDispositivo([FromBody] TokenDispositivoModel model)
        {
            await _firestoreService.ActualizarTokenDispositivo(model.GuardianId, model.Token);
            return Ok(new { message = "Token de dispositivo actualizado correctamente" });
        }

        // Eliminar el token de dispositivo
        [HttpDelete("eliminarTokenDispositivo/{guardianId}")]
        public async Task<IActionResult> EliminarTokenDispositivo(string guardianId)
        {
            await _firestoreService.EliminarTokenDispositivo(guardianId);
            return Ok(new { message = "Token de dispositivo eliminado correctamente" });
        }
    }

    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class TokenDispositivoModel
    {
        public string GuardianId { get; set; }
        public string Token { get; set; }
    }
}