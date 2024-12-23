using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using CsCrudApi.Models.UserRelated;
using Microsoft.AspNetCore.Mvc;
using CsCrudApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CsCrudApi.Services
{
    public static class TokenServices
    {

        public static string GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = GetKey();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.TipoInteresse.ToString())
                ]),
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

        public static async Task<User?> GetTokenUserAsync(ClaimsPrincipal claimsPrincipal, ApplicationDbContext context)
        {
            if (claimsPrincipal == null)
            {
                return null;
            }

            var emailClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                return null;
            }

            return await context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);
        }

        public static string GenerateGUIDString() => Guid.NewGuid().ToString("N");
    }
}
