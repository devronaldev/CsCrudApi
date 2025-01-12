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
using System.Net.Mime;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FileServices _fileServices;

        public UserController(ApplicationDbContext context, FileServices fileServices)
        {
            _context = context;
            _fileServices = fileServices;
        }

        [HttpGet("perfil/{userId}")]
        [AllowAnonymous]
        public async Task<ActionResult<dynamic>> Profile([FromRoute] int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

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

            PostController postController = new(_context);

            return new
            {
                IdUsuario = user.UserId,
                NmUsuario = user.NmSocial,
                user.DtNasc,
                user.Email,
                user.ProfilePictureUrl,
                TpPreferencia = user.TipoInteresse,
                user.CursoId,
                user.GrauEscolaridade,
                Seguidores = followers,
                Seguindo = following,
                Cidade = cidade.Name,
                NmInstituicao = $"{campus.SgCampus} - {campus.CampusName}",
                IdCampus = campus.Id
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

            (emailVerification.NewEmail, user.Email) = (user.Email, emailVerification.NewEmail);
            emailVerification.IsVerified = true;
            emailVerification.ExpiresAt = DateTime.Now.AddMonths(1);

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("seguir")]
        public async Task<ActionResult<dynamic>> Follow([FromHeader] string token, [FromRoute] int userId)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new
                {
                    Message = "O token não pode estar vazio."
                });
            }

            try
            {
                // Validação do token e obtenção do usuário autenticado
                var user = await TokenServices.GetTokenUserAsync(TokenServices.ValidateJwtToken(token), _context);
                if (user == null)
                {
                    return NotFound(new
                    {
                        Message = "Usuário não encontrado."
                    });
                }

                if (user.UserId == userId)
                {
                    return Conflict(new
                    {
                        Message = "O usuário não pode seguir a si mesmo."
                    });
                }

                if (!await _context.Users.AnyAsync(u => u.UserId == userId))
                {
                    return NotFound(new
                    {
                        Message = "Usuário a ser seguido não existe."
                    });
                }

                // Busca por uma relação existente
                var follow = await _context.UsersFollowing.SingleOrDefaultAsync(f =>
                    f.CdFollower == user.UserId && f.CdFollowed == userId);

                if (follow != null)
                {
                    follow.Status = !follow.Status;
                    follow.LastUpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        Message = follow.Status
                            ? "Usuário seguido com sucesso."
                            : "Você deixou de seguir o usuário.",
                        Data = follow
                    });
                }

                // Cria uma nova relação se não existir
                var action = new UserFollowingUser
                {
                    CdFollowed = userId,
                    CdFollower = user.UserId,
                    Status = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };

                _context.UsersFollowing.Add(action);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Usuário seguido com sucesso.",
                    Data = action
                });
            }
            catch (Exception ex)
            {
                // Mensagem genérica para segurança em produção
                return StatusCode(500, new
                {
                    Message = "Ocorreu um erro interno. Tente novamente mais tarde.",
                    Details = ex.Message
                });
            }
        }


        [Produces("application/json")]
        [Consumes("multipart/form-data")]
        [HttpPost("foto-perfil")]
        public async Task<ActionResult> UpdateProfilePicture([FromForm] string profilePicture, [FromHeader] string token)
        {
            if(profilePicture == null || profilePicture.Length == 0)
            {
                return BadRequest(new { message = "A foto de perfil é obrigatória." });
            }

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "O token não pode estar vazio." });
            }

            /*
            if (!FileServices.AllowedProfileContentTypes.Contains(profilePicture.ContentType))
            {
                Console.WriteLine($"O tipo de arquivo não é suportável. Verifique os detalhes: {profilePicture.ContentType} - {profilePicture.FileName}");
                return BadRequest(new { message = "Formato de arquivo não suportado." });
            }
            */

            try
            {
                // Validação Token
                var user = await TokenServices.GetTokenUserAsync(TokenServices.ValidateJwtToken(token), _context);
                if (user == null)
                {
                    return NotFound("Token inválido/expirado ou usuário não encontrado.");
                }

                // Enviar arquivo para S3
                var fileName = $"profilePictures/{TokenServices.GenerateGUIDString()}";
                /*
                using var stream = profilePicture.OpenReadStream();
                var fileUrl = await _fileServices.UploadFileAsync(stream, fileName, profilePicture.ContentType);
                

                if (string.IsNullOrEmpty(fileUrl))
                {
                    return StatusCode(500, new { message = "Erro inesperado ao salvar o arquivo." });
                }

                user.ProfilePictureUrl = fileUrl;
                */
                await _context.SaveChangesAsync();

                return Ok("Foto de perfil alterado com sucesso");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro inesperado. Detalhes: {ex.Message}");
            }
        }

        [HttpGet("buscar")]
        public async Task<ActionResult<List<object>>> SearchUsersByName([FromQuery] string namePart, int pageNumber, int pageSize)
        {
            if (string.IsNullOrEmpty(namePart))
            {
                return BadRequest(new
                {
                    Message = "O campo de busca não pode estar vazio."
                });
            }

            pageSize = pageSize > 10 ? 10 : (pageSize < 1 ? 1 : pageSize);
            pageNumber = pageNumber < 1 ? 1 : pageNumber;

            try
            {
                var users = await _context.Users
                    .Where(u => EF.Functions.Like(u.NmSocial, $"%{namePart}%")&& u.IsEmailVerified == true)
                    .Select(u => new
                    {
                        Id = u.UserId,
                        Nome = u.NmSocial,
                    })
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (users.Count == 0)
                {
                    return NotFound(new
                    {
                        Message = "Nenhum usuário encontrado."
                    });
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro: {ex.Message}");
            }
        }

        protected async Task<int> GetFollowers(int idUser) => await _context.UsersFollowing.CountAsync(u => u.CdFollowed == idUser);

        protected async Task<int> GetFollowing(int idUser) => await _context.UsersFollowing.CountAsync(u => u.CdFollower == idUser);
    }
}
