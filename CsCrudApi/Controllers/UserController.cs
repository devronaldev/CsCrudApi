using BCrypt.Net;
using CsCrudApi.Models;
using CsCrudApi.Models.UserRelated;
using CsCrudApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Services;
using System.IdentityModel.Tokens.Jwt;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public UserController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("perfil")]
        public async Task<ActionResult<dynamic>> Profile([FromBody] int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == id);

            if (user == null)
            {
                return NotFound("Usuário não encontrado ou inexistente.");
            }
            
            var cidade = await _context.Cidades.FirstOrDefaultAsync(c => c.IdCidade == user.CdCidade);

            if (cidade == null) 
            {
                return NotFound("Usuário com cidade não cadastrada ou cadastrado incorretamente.");
            }

            var campus = await _context.Campi.FirstOrDefaultAsync(campi => campi.Id == user.CdCampus);

            if (campus == null)
            {
                return NotFound("Campus não encontrado ou cadastrado incorretamente");
            }

            var cidadeCampus = await _context.Cidades.FirstOrDefaultAsync(c => c.IdCidade == campus.CdCidade);

            if (cidadeCampus == null)
            {
                return NotFound("Campus com cidade não cadastrada ou cadastrado incorretamente.");
            }
            
            int followers = await _context.UsersFollowing.CountAsync(u => u.CdFollowed == user.IdUser);

            return new 
            {
                user.IdUser,
                user.NmSocial,
                user.DtNasc,
                user.Email,
                user.TpPreferencia,
                user.DescTitulo,
                followers,
                cidade,
                campus.Id,
                campus.SgCampus,
                campus.CampusName,
                cidadeCampus
            };
        }

        [HttpPost("atualizar-senha")]
        public async Task<ActionResult<dynamic>> ChangePassword([FromHeader] string token, [FromBody] ChangePasswordRequest request)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = TokenServices.GetKey();

            try
            {
                // Validando o token
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero // Para evitar problemas de expiração
                }, out SecurityToken validatedToken);

                //Verificações de token
                var jwtToken = validatedToken as JwtSecurityToken;
                if (jwtToken == null)
                {
                    return BadRequest("Token inválido: não é um JWT.");
                }

                var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                if (string.IsNullOrEmpty(emailClaim))
                {
                    return BadRequest("Token inválido, claim de e-mail ausente.");
                }

                //Verificações de requisição
                if (string.IsNullOrEmpty(request.OldPassword))
                {
                    return BadRequest("Senha anterior ausente.");
                }

                if (string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest("Senha nova ausente.");
                }

                if (!request.NewPassword.Equals(request.ConfirmPassword))
                {
                    return BadRequest("Senhas informadas não correspondem.");
                }

                //Verificações de usuário
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);

                if (user == null)
                {
                    return NotFound("Usuário não encontrado.");
                }

                if(!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.Password))
                {
                    return Unauthorized("Senha incorreta.");
                }

                //Update no banco de dados
                var newPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.Password = newPassword;
                await _context.SaveChangesAsync();

                //Excluir instâncias de senhas
                user.Password = "";
                request.NewPassword = "";
                request.OldPassword = "";
                request.ConfirmPassword = "";

                await EmailServices.ChangePasswordAdvice(user, DateTime.Now);

                token = TokenServices.GenerateToken(user);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro na atualização: {ex.Message}");
            }  
        }
    }
}
