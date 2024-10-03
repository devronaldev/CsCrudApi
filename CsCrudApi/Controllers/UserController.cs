using BCrypt.Net;
using CsCrudApi.Models;
using CsCrudApi.Models.UserRelated;
using CsCrudApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CsCrudApi.Services;
using System.IdentityModel.Tokens.Jwt;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

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
                if (validatedToken as JwtSecurityToken == null)
                {
                    return BadRequest("Token inválido: não é um JWT.");
                }

                var emailClaim = (validatedToken as JwtSecurityToken).Claims.FirstOrDefault(c => c.Type == "email")?.Value;
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

        [HttpPost("atualizar-email")]
        public async Task<ActionResult<dynamic>> ChangeEmail([FromHeader] string token, [FromBody] ChangeEmailRequest request)
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
                if (validatedToken is not JwtSecurityToken jwtToken)
                {
                    return BadRequest("Erro: Token inválido: não é um JWT.");
                }

                var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                if (string.IsNullOrEmpty(emailClaim))
                {
                    return BadRequest("Erro: Token inválido, claim de e-mail ausente.");
                }

                //Validar e-mails
                if (string.IsNullOrEmpty(request.Email)) 
                {
                    return BadRequest("Erro: E-mail vazio");
                }

                if (!string.Equals(request.EmailConfirm, request.Email))
                {
                    return BadRequest("Erro: E-mails diferentes.");
                }

                //Instanciando no banco de dados.
                var user  = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);

                if (user == null)
                {
                    return NotFound("Erro: E-mail não cadastrado.");
                }

                if (user.Email.Equals(request.Email))
                {
                    //NÃO ACREDITO QUE VOU FAZER ISSO
                    return Conflict("Erro: Novo e-mail não pode ser igual ao anterior");
                }
    
                await EmailServices.ChangeEmailAdvice(user, DateTime.Now);

                var emailVerification = new EmailVerification
                {
                    UserId = user.IdUser,
                    NewEmail = request.Email,
                    VerificationToken = GenerateVerificationToken(),
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddHours(1)
                };

                _context.EmailVerifications.Add(emailVerification);
                await _context.SaveChangesAsync();

                user = new User();

                await EmailServices.ChangeEmailVerification(emailVerification, DateTime.Now);
                return Ok("Envio de e-mail de verificação realizado com sucesso.");
            }
            catch (Exception ex) 
            {
                return BadRequest($"Erro: {ex.Message}");
            }
        }

        private string GenerateVerificationToken()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
