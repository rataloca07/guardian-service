using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GuardianService.Services
{
    public interface IAuthService
    {
        string GenerateToken(string guardianId);
    }

    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(string guardianId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // Obtener el valor de la clave desde la configuración
            //var keyString = _configuration["Jwt:Key"];
            //            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var keyString = _configuration["Jwt:Key"];

            if (string.IsNullOrEmpty(keyString))
            {
                keyString = Environment.GetEnvironmentVariable("JWT_KEY");
            }

            // Imprimir el valor de la clave obtenida de la variable de entorno o configuración
            Console.WriteLine("Valor de Jwt:Key obtenido: " + keyString);

            // Verificar si keyString sigue siendo nulo o vacío
            if (string.IsNullOrEmpty(keyString))
            {
                throw new Exception("Jwt:Key es nulo o vacío.");
            }

            // Convertir la clave a bytes
            var key = Encoding.UTF8.GetBytes(keyString);

            // Si tienes algún servicio de logging, podrías usarlo en lugar de Console.WriteLine
            // _logger.LogInformation("Valor de Jwt:Key obtenido: " + keyString);

            // Convertir la clave de Base64 a un arreglo de bytes
            //var key = Convert.FromBase64String(keyString);

            var Issuer = _configuration["Jwt:Issuer"];
            var Audience = _configuration["Jwt:Audience"];
            if (String.IsNullOrEmpty(Issuer))
            {
                Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            }
            if (String.IsNullOrEmpty(Audience))
            {
                Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
            }

            /*if (String.IsNullOrEmpty(keyString))
            {
                keyString = Environment.GetEnvironmentVariable("JWT_KEY");
            }*/

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, guardianId)
        }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = Issuer,
                Audience = Audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
