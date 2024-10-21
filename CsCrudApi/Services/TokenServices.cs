using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using CsCrudApi.Models.UserRelated;

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
            var secret = Environment.GetEnvironmentVariable("SECRET");

            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException("Chave secreta não configurada no Ambiente");
            }

            return Encoding.ASCII.GetBytes(secret);
        }

        public static ClaimsPrincipal? ValidateJwtToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = GetKey();

            try
            {
                // Configura os parâmetros de validação
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false, // Se quiser validar o emissor, altere para true
                    ValidateAudience = false, // Se quiser validar a audiência, altere para true
                    ClockSkew = TimeSpan.Zero // Define o tempo de tolerância para expiração
                };

                // Valida o token e obtém as informações de segurança
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Retorna as "claims" associadas ao token
                return principal;
            }
            catch (Exception ex)
            {
                // Caso a validação falhe, você pode tratar o erro aqui, logar a exceção, etc.
                Console.WriteLine($"Erro de validação do token: {ex.Message}");
                return null;
            }
        }

        public static string GenerateGUIDString() => Guid.NewGuid().ToString("N");
    }
}
