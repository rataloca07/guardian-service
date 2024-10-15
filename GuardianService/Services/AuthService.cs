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

            if (string.IsNullOrEmpty(keyString))
            {
                keyString = Environment.GetEnvironmentVariable("JWT_KEY");
            }

            Console.WriteLine("Valor de Jwt:Key obtenido: " + keyString);

            // Verificar si keyString sigue siendo nulo o vacío
            if (string.IsNullOrEmpty(keyString))
            {
                throw new Exception("Jwt:Key es nulo o vacío.");
            }

            // Convertir la clave a bytes
            var key = Encoding.UTF8.GetBytes(keyString);

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


            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, guardianId)
        }),
                Expires = DateTime.UtcNow.AddHours(720),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = Issuer,
                Audience = Audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
