using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using CsCrudApi.Models.User;

namespace CsCrudApi.Services
{
    public static class TokenServices
    {
        private static IConfiguration _configuration;
        static TokenServices()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _configuration = configurationBuilder;
        }

        public static string GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = GetKey();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.TpPreferencia.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            }; 

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public static byte[] GetKey()
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("A configuração não foi inicializada.");
            }

            var secret = _configuration["Secret"];

            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException("Chave secreta não configurada no appsettings.json.");
            }

            return Encoding.ASCII.GetBytes(secret);
        }
    }
}
