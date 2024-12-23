using CsCrudApi.Models;
using CsCrudApi.Models.UserRelated;
using CsCrudApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.UserRelated.Request;
using Microsoft.AspNetCore.Authorization;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

        [HttpGet("perfil/{userId}")]
        [AllowAnonymous]
        public async Task<ActionResult<dynamic>> Profile([FromRoute] int userId, [FromQuery] int pageSize, int pageNumber)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 10 : pageSize;

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

            int followers = await GetFollowers(user.UserId);

            int following = await GetFollowing(user.UserId);

            PostController postController = new PostController(_context);
            var posts = await postController.GetUserPosts(user.UserId, pageNumber, pageSize);

            return new
            {
                IdUsuario = user.UserId,
                NmUsuario = user.NmSocial,
                user.DtNasc,
                user.Email,
                //user.FtPerfil, 
                TpPreferencia = user.TipoInteresse,
                Seguidores = followers,
                Seguindo = following,
                Cidade = cidade.Name,
                NmInstituicao = $"{campus.SgCampus} - {campus.CampusName}",
                IdCampus = campus.Id,
                Posts = posts
            };
        }

        [Authorize]
        [RequireHttps]
        [HttpPatch("atualizar-senha")]
        public async Task<ActionResult<dynamic>> ChangePassword([FromHeader] string token, [FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new
                {
                    Message = "Erro: Token vazio."
                });
            }

            if (request == null)
            {
                return BadRequest(new
                {
                    Message = "Requisição vazia."
                });
            }

            try
            {
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
                var user = await TokenServices.GetTokenUserAsync(claimsPrincipal: TokenServices.ValidateJwtToken(token), _context);
                if (user == null)
                {
                    return BadRequest(new
                    {
                        message = "Erro na identificação do usuário"
                    });
                }

                if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.Password))
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

        [Authorize]
        [RequireHttps]
        [HttpPatch("atualizar-email")]
        public async Task<ActionResult<dynamic>> ChangeEmailRequest([FromHeader] string token, [FromBody] ChangeEmailRequest request)
        {
            if (string.IsNullOrEmpty(token)) { NotFound(new { message = $"Erro: Token '{token}' vazio" }); }
            try
            {
                //Validar e-mails
                if (string.IsNullOrEmpty(request.Email)) 
                {
                    return BadRequest("Erro: E-mail vazio");
                }
                if (!string.Equals(request.EmailConfirm, request.Email))
                {
                    return BadRequest("Erro: E-mails diferentes.");
                }

                //Verificações de usuário
                var user = await TokenServices.GetTokenUserAsync(claimsPrincipal: TokenServices.ValidateJwtToken(token), _context);
                if (user == null)
                {
                    return BadRequest(new
                    {
                        message = "Erro na identificação do usuário"
                    });
                }

                if (user.Email.Equals(request.Email))
                {
                    //NÃO ACREDITO QUE VOU FAZER ISSO
                    return Conflict("Erro: Novo e-mail não pode ser igual ao anterior");
                }
    
                await EmailServices.ChangeEmailAdvice(user, DateTime.Now, request.Email);

                var emailVerification = new EmailVerification
                {
                    UserId = user.UserId,
                    NewEmail = request.Email,
                    VerificationToken = TokenServices.GenerateGUIDString(),
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddHours(1)
                };

                _context.EmailVerifications.Add(emailVerification);
                await _context.SaveChangesAsync();

                user = new User();

                await EmailServices.ChangeEmailVerification(emailVerification);
                return Ok("Envio de e-mail de verificação realizado com sucesso.");
            }
            catch (Exception ex) 
            {
                return BadRequest($"Erro: {ex.Message}");
            }
        }

        [Authorize]
        [RequireHttps]
        [HttpPatch("trocar-email")]
        public async Task<ActionResult<dynamic>> ChangeEmailVerification([FromQuery] string token)
        {
            var emailVerification = await _context.EmailVerifications.FirstOrDefaultAsync(e => e.VerificationToken == token);
            if (emailVerification == null)
            {
                return NotFound("Erro: Token inválido ou expirado");
            }
            if (emailVerification.ExpiresAt < DateTime.Now) 
            {
                return StatusCode(410, "Erro: A requisição de troca de e-mail expirou.");
            }

            int doesEmailExist = await _context.Users.CountAsync(u => u.Email == emailVerification.NewEmail);
            if (doesEmailExist != 0)
            {
                return Conflict("Erro: O e-mail já está conectado a outro usuário.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == emailVerification.UserId);
            if (user == null)
            {
                return NotFound("Erro: Usuário não encontrado");
            }

            string previousEmail = user.Email;
            user.Email = emailVerification.NewEmail;
            emailVerification.NewEmail = previousEmail;
            emailVerification.IsVerified = true;
            emailVerification.ExpiresAt = DateTime.Now.AddMonths(1);

            await _context.SaveChangesAsync();
            return Ok();
        }

        protected async Task<int> GetFollowers(int idUser) => await _context.UsersFollowing.CountAsync(u => u.CdFollowed == idUser);

        protected async Task<int> GetFollowing(int idUser) => await _context.UsersFollowing.CountAsync(u => u.CdFollower == idUser);
    }
}
