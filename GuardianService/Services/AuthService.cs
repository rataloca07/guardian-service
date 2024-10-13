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
            var keyString = _configuration["Jwt:Key"];

            // Imprimir el valor de la clave obtenida de la configuración
            Console.WriteLine("Valor de Jwt:Key obtenido: " + keyString);

            // Si tienes algún servicio de logging, podrías usarlo en lugar de Console.WriteLine
            // _logger.LogInformation("Valor de Jwt:Key obtenido: " + keyString);

            // Convertir la clave de Base64 a un arreglo de bytes
            var key = Convert.FromBase64String(keyString);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, guardianId)
        }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
