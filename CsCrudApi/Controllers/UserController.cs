using System.Security.Claims;
using CsCrudApi.Models;
using CsCrudApi.Models.UserRelated;
using CsCrudApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CsCrudApi.Models.UserRelated.Request;
using Microsoft.AspNetCore.Authorization;
using CsCrudApi.DTOs;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retorna os detalhes do perfil de um usuário específico.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite que qualquer pessoa visualize informações públicas de um perfil de usuário,
        /// incluindo nome social, data de nascimento, e-mail, URL da foto de perfil,
        /// preferências, curso, escolaridade, campus e a contagem de seguidores e de quem o usuário segue.
        /// Retorna um erro 404 se o usuário, a cidade ou o campus associado não forem encontrados no sistema.
        /// </remarks>
        /// <param name="userId">O ID único do usuário cujo perfil será consultado.</param>
        /// <returns>
        /// Retorna uma instância de <see cref="UserProfileDTO"/> se o perfil for encontrado.
        /// Retorna <see cref="StatusCodes.Status404NotFound"/> com um <see cref="ErrorDTO"/>
        /// se o usuário, a cidade ou o campus não existirem.
        /// </returns>
        /// <response code="200">Retorna com sucesso o <see cref="UserProfileDTO"/> com os dados do perfil.</response>
        /// <response code="404">Retorna <see cref="ErrorDTO"/> indicando que o usuário, cidade ou campus não foi encontrado.</response>
        [HttpGet("perfil/{userId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserProfileDTO), StatusCodes.Status200OK)] // Agora retorna UserProfileDTO
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProfileDTO>> Profile([FromRoute] int userId) // Tipo de retorno explícito
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new ErrorDTO
                {
                    ErrorCode = "USER404",
                    Message = "Usuário não encontrado.",
                    ErrorDescription = $"Não foi encontrado usuário com o ID '{userId}'."
                });
            }

            var cidade = await _context.Cidades.FirstOrDefaultAsync(c => c.IdCidade == user.CdCidade);

            if (cidade == null)
            {
                return NotFound(new ErrorDTO
                {
                    ErrorCode = "CITY404",
                    Message = "Cidade do usuário não encontrada.",
                    ErrorDescription = $"A cidade associada ao usuário (ID: {user.CdCidade}) não foi encontrada ou está incorreta."
                });
            }

            var campus = await _context.Campi.FirstOrDefaultAsync(campi => campi.Id == user.CdCampus);

            if (campus == null)
            {
                return NotFound(new ErrorDTO
                {
                    ErrorCode = "CAMPUS404",
                    Message = "Campus do usuário não encontrado.",
                    ErrorDescription = $"O campus associado ao usuário (ID: {user.CdCampus}) não foi encontrado ou está incorreto."
                });
            }

            var followers = await GetFollowers(user.UserId);
            var following = await GetFollowing(user.UserId);

            // Usa o construtor do DTO para criar a instância
            var userProfile = new UserProfileDTO(user, cidade, campus, followers, following);

            return Ok(userProfile);
        }

        /// <summary>
        /// Permite que um usuário autenticado altere sua senha.
        /// </summary>
        /// <remarks>
        /// Este endpoint requer autenticação (JWT no cabeçalho 'Authorization').
        /// A nova senha deve ser diferente da antiga e a confirmação deve ser idêntica à nova senha.
        /// Em caso de sucesso, um novo token JWT é gerado e retornado, invalidando o anterior.
        /// É enviado um e-mail de notificação ao usuário após a alteração.
        /// </remarks>
        /// <param name="token">O token JWT do usuário, geralmente fornecido via cabeçalho 'Authorization: Bearer &lt;token&gt;'.
        /// Embora o atributo [Authorize] trate a autenticação, este parâmetro pode ser usado para acessar o token diretamente.</param>
        /// <param name="request">Um objeto <see cref="ChangePasswordRequest"/> contendo a senha antiga, nova senha e confirmação da nova senha.</param>
        /// <returns>
        /// Retorna:
        /// <list type="bullet">
        /// <item><description><see cref="OkObjectResult"/> com <see cref="SuccessDTO"/> (contendo o novo token JWT) em caso de sucesso.</description></item>
        /// <item><description><see cref="BadRequestObjectResult"/> com <see cref="ErrorDTO"/> se a requisição for inválida (senhas ausentes, não correspondentes, usuário não encontrado).</description></item>
        /// <item><description><see cref="UnauthorizedResult"/> ou <see cref="UnauthorizedObjectResult"/> com <see cref="ErrorDTO"/> se a senha antiga estiver incorreta ou se o usuário não estiver autenticado.</description></item>
        /// <item><description><see cref="StatusCodeResult"/> com <see cref="StatusCodes.Status500InternalServerError"/> com <see cref="ErrorDTO"/> em caso de erro interno no servidor.</description></item>
        /// </list>
        /// </returns>
        /// <response code="200">Senha alterada com sucesso. Retorna um <see cref="SuccessDTO"/> com o novo token JWT.</response>
        /// <response code="400">Dados da requisição inválidos (ex: senhas vazias, senhas não conferem) ou erro na identificação do usuário. Retorna <see cref="ErrorDTO"/>.</response>
        /// <response code="401">Não autorizado (token JWT ausente ou inválido).</response>
        /// <response code="403">Acesso negado (se o usuário não tiver permissão, embora não esteja explícito neste código).</response>
        /// <response code="500">Erro interno do servidor durante a atualização da senha. Retorna <see cref="ErrorDTO"/>.</response>
        [Authorize]
        //[RequireHttps]
        [HttpPatch("atualizar-senha")]
        [ProducesResponseType(typeof(SuccessDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // 401 para falha de autenticação
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SuccessDTO>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // Sugestão: Use ModelState.IsValid para validações básicas de DTO.
            // As anotações [Required], [Compare] etc. no ChangePasswordRequest
            // farão com que o ModelState.IsValid seja false se as validações falharem.
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "VALIDATION400",
                    Message = "Dados da requisição inválidos.",
                    ErrorDescription = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage))
                });
            }
            
            ClaimsPrincipal claimsPrincipal = HttpContext.User; // Acessa o ClaimsPrincipal do usuário autenticado via [Authorize]

            if (claimsPrincipal == null || !claimsPrincipal.Identity.IsAuthenticated)
            {
                // Isso geralmente não deveria acontecer se [Authorize] estiver funcionando,
                // mas é um fallback seguro.
                return Unauthorized(new ErrorDTO
                {
                    ErrorCode = "AUTH401",
                    Message = "Não autorizado.",
                    ErrorDescription = "Token de autenticação ausente ou inválido."
                });
            }
            
            var user = await TokenServices.GetTokenUserAsync(claimsPrincipal: claimsPrincipal, _context);

            if (user == null)
            {
                return Unauthorized(new ErrorDTO 
                {
                    ErrorCode = "USERAUTH401",
                    Message = "Erro na identificação do usuário.",
                    ErrorDescription = "O token de autenticação não corresponde a um usuário válido ou ativo."
                });
            }

            // Validação da senha antiga
            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.Password))
            {
                return Unauthorized(new ErrorDTO
                {
                    ErrorCode = "AUTH401PASS",
                    Message = "Senha antiga incorreta.",
                    ErrorDescription = "A senha antiga fornecida não corresponde à senha registrada para o usuário."
                });
            }

            try
            {
                // Update no banco de dados
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.Password = newPasswordHash;
                await _context.SaveChangesAsync();

                // Envia e-mail de notificação
                await EmailServices.ChangePasswordAdvice(user, DateTime.Now);

                // Gerar e retornar um novo token JWT
                string newToken = TokenServices.GenerateToken(user);
                return Ok(new SuccessDTO(newToken, "Token"));
            }
            catch (DbUpdateException dbEx) // Captura exceções específicas de banco de dados
            {
                //TODO: Log do erro
                Console.WriteLine($"Erro de banco de dados ao atualizar senha: {dbEx.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDTO
                {
                    ErrorCode = "DB500",
                    Message = "Erro interno no servidor ao atualizar a senha.",
                    ErrorDescription =
                        "Ocorreu um erro ao persistir a nova senha no banco de dados. Tente novamente mais tarde."
                });
            }
            catch (Exception ex)
            {
                //TODO: Log do erro
                Console.WriteLine($"Erro inesperado ao atualizar senha: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDTO
                {
                    ErrorCode = "GENERIC500",
                    Message = "Ocorreu um erro inesperado ao processar a requisição.",
                    ErrorDescription =
                        "Por favor, tente novamente mais tarde. Se o problema persistir, contate o suporte."
                });
            }
        }

        [Authorize]
        //[RequireHttps]
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

        [HttpPost("seguir/{userId}")]
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
        public async Task<ActionResult> UpdateProfilePicture([FromBody] string profilePicture, [FromHeader] string token)
        {
            if(string.IsNullOrEmpty(profilePicture))
            {
                return BadRequest(new { message = "A foto de perfil é obrigatória." });
            }

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "O token não pode estar vazio." });
            }

            try
            {
                // Validação Token
                var user = await TokenServices.GetTokenUserAsync(TokenServices.ValidateJwtToken(token), _context);
                if (user == null)
                {
                    return NotFound("Token inválido/expirado ou usuário não encontrado.");
                }
                
                user.ProfilePictureUrl = profilePicture;
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
                        u.ProfilePictureUrl
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
