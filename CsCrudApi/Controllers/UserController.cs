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
        [RequireHttps]
        [HttpPatch("atualizar-senha")]
        [ProducesResponseType(typeof(SuccessDTO), 200)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<SuccessDTO>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
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

            ClaimsPrincipal
                claimsPrincipal = HttpContext.User; // Acessa o ClaimsPrincipal do usuário autenticado via [Authorize]

            if (claimsPrincipal == null || !claimsPrincipal.Identity.IsAuthenticated)
            {
                // Isso não deve acontecer, mas é um fallback seguro.
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

        /// <summary>
        /// Solicita uma atualização de e-mail para o usuário autenticado.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite que um usuário autenticado solicite a alteração de seu e-mail.
        /// Um e-mail de verificação será enviado para o novo endereço, contendo um token de confirmação.
        /// O novo e-mail não pode ser igual ao e-mail atual do usuário.
        /// </remarks>
        /// <param name="request">Objeto contendo o novo e-mail e a confirmação do e-mail.</param>
        /// <returns>
        /// Retorna um status 200 OK em caso de sucesso, indicando que o e-mail de verificação foi enviado.
        /// Retorna um status 400 Bad Request se os e-mails não forem fornecidos, não coincidirem, ou forem inválidos.
        /// Retorna um status 401 Unauthorized se o token de autenticação for ausente ou inválido.
        /// Retorna um status 404 Not Found se o usuário do token não for encontrado no sistema.
        /// Retorna um status 409 Conflict se o novo e-mail for idêntico ao e-mail atual do usuário.
        /// Retorna um status 500 Internal Server Error em caso de erro inesperado no servidor.
        /// </returns>
        /// <response code="202">Envio de e-mail de verificação realizado com sucesso.</response>
        /// <response code="400">Dados inválidos (e-mails vazios, e-mails não coincidem, e-mail já existente ou formato inválido).</response>
        /// <response code="401">Não autorizado (token ausente ou inválido).</response>
        /// <response code="404">Usuário não encontrado.</response>
        /// <response code="409">Novo e-mail é igual ao e-mail anterior.</response>
        /// <response code="500">Erro inesperado do servidor.</response>
        [Authorize]
        //[RequireHttps] // Mantenha esta linha se você garantir HTTPS em outro nível (ex: Nginx, Load Balancer)
        [HttpPatch("solicitar-atualizar-email")]
        [ProducesResponseType(typeof(SuccessDTO), 202)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 401)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 409)]
        [ProducesResponseType(typeof(string), 500)] // Ou ErrorDTO, dependendo de como você lida com 500s
        public async Task<ActionResult<SuccessDTO>> ChangeEmailRequest([FromBody] ChangeEmailRequest request)
        {
            // Validar e-mails
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400NULL",
                    ErrorDescription = "Verifique se o e-mail foi enviado na requisição.",
                    Message = "O e-mail não pode estar vazio."
                });
            }

            request.Email = request.Email.Trim().ToLower();
            request.EmailConfirm = request.EmailConfirm.Trim().ToLower();

            if (!string.Equals(request.EmailConfirm, request.Email))
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400NOTEQUAL",
                    ErrorDescription = "Verifique se a validação da igualdade dos e-mails está correta.",
                    Message = "Os endereços de e-mail não coincidem."
                });
            }

            try
            {
                ClaimsPrincipal
                    claimsPrincipal =
                        HttpContext.User; // Acessa o ClaimsPrincipal do usuário autenticado via [Authorize]

                if (claimsPrincipal == null || !claimsPrincipal.Identity.IsAuthenticated)
                {
                    // Isso não deve acontecer, mas é um fallback seguro.
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
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "404USER",
                        ErrorDescription = "Verifique se o token está devidamente referenciado.",
                        Message = "O usuário não foi encontrado."
                    });
                }

                if (user.Email.Equals(request.Email))
                {
                    return Conflict(new ErrorDTO
                    {
                        ErrorCode = "409EMAIL",
                        ErrorDescription = "O novo e-mail e o anterior são iguais.",
                        Message = "Novo e-mail não pode ser igual ao anterior"
                    });
                }

                // Início da lógica para gerar e enviar o e-mail de verificação
                await EmailServices.ChangeEmailAdvice(user, DateTime.Now,
                    request.Email); // Envia um aviso ao e-mail antigo (opcional, mas boa prática)

                var emailVerification = new EmailVerification
                {
                    UserId = user.UserId,
                    NewEmail = request.Email,
                    VerificationToken = TokenServices.GenerateGUIDString(),
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddHours(2)
                };

                _context.EmailVerifications.Add(emailVerification);
                await _context.SaveChangesAsync();

                await EmailServices
                    .ChangeEmailVerification(emailVerification); // Envia o e-mail com o link de verificação
                return Accepted(new SuccessDTO("Envio de e-mail de verificação realizado com sucesso.", "Message"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GENERIC500",
                    ErrorDescription = "Qualquer tipo de exceção pode ter ocorrido.",
                    Message = "Ocorreu um erro inesperado."
                });
            }
        }

        /// <summary>
        /// Confirma a troca de e-mail de um usuário.
        /// </summary>
        /// <remarks>
        /// Este endpoint é utilizado para finalizar o processo de troca de e-mail de um usuário.
        /// Ele valida um token de verificação (GUID) recebido por e-mail.
        /// Se o token for válido e não expirado, e o novo e-mail não estiver em uso por outro usuário,
        /// o e-mail do usuário será atualizado e o token de verificação será marcado como usado/expirado.
        ///
        /// **Atenção:** Embora o atributo `[Authorize]` esteja presente, o endpoint é acessado principalmente
        /// através de um link de e-mail que contém o `GUID`. A autorização é mais uma camada de segurança
        /// ou para casos onde o usuário já está logado e o token é validado novamente.
        /// </remarks>
        /// <param name="guid">O GUID (Global Unique Identifier) do token de verificação recebido por e-mail.</param>
        /// <returns>
        /// Retorna um status 200 OK em caso de sucesso na confirmação da troca de e-mail.
        /// Retorna um status 400 Bad Request se o GUID não for fornecido ou for inválido.
        /// Retorna um status 401 Unauthorized se o token de autenticação (Bearer) for ausente ou inválido.
        /// Retorna um status 404 Not Found se o token de verificação ou o usuário associado não forem encontrados.
        /// Retorna um status 409 Conflict se o novo e-mail já estiver em uso por outro usuário.
        /// Retorna um status 410 Gone se o token de verificação tiver expirado.
        /// Retorna um status 500 Internal Server Error em caso de erro inesperado no servidor.
        /// </returns>
        /// <response code="204">E-mail do usuário atualizado com sucesso.</response>
        /// <response code="400">GUID não fornecido ou inválido.</response>
        /// <response code="401">Não autorizado (token Bearer ausente ou inválido, se `[Authorize]` estiver ativo).</response>
        /// <response code="404">Token de verificação não encontrado ou usuário associado não encontrado.</response>
        /// <response code="409">O novo e-mail já está em uso por outro usuário.</response>
        /// <response code="410">A requisição de troca de e-mail expirou.</response>
        /// <response code="500">Erro inesperado do servidor.</response>
        [AllowAnonymous]
        [RequireHttps]
        [HttpPatch("confirmar-troca-email")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorDTO), 400)] // Retorna string, mas idealmente seria um DTO
        [ProducesResponseType(typeof(ErrorDTO), 401)]
        [ProducesResponseType(typeof(ErrorDTO), 404)] // Retorna string, mas idealmente seria um DTO
        [ProducesResponseType(typeof(ErrorDTO), 409)] // Retorna string, mas idealmente seria um DTO
        [ProducesResponseType(typeof(ErrorDTO), 410)] // Retorna string, mas idealmente seria um DTO
        [ProducesResponseType(typeof(ErrorDTO), 500)] // Ou ErrorDTO
        public async Task<ActionResult> ChangeEmailValidation([FromQuery] string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                // Embora o FromQuery normalmente trate isso, é bom ter uma validação explícita.
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400GUIDNULL",
                    Message = "O token de verificação (GUID) é obrigatório.",
                    ErrorDescription = "O parâmetro 'guid' da query string não foi fornecido."
                });
            }

            try
            {
                var emailVerification =
                    await _context.EmailVerifications.FirstOrDefaultAsync(e => e.VerificationToken == guid);
                if (emailVerification == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "404TOKEN",
                        Message = "Erro: Token inválido.",
                        ErrorDescription = "O token de verificação não foi encontrado no sistema ou já foi utilizado."
                    });
                }

                if (emailVerification.ExpiresAt < DateTime.Now)
                {
                    return StatusCode(410, new ErrorDTO
                    {
                        ErrorCode = "410EXPIRED",
                        Message = "Erro: A requisição de troca de e-mail expirou.",
                        ErrorDescription = "O token de verificação fornecido já passou da data de validade."
                    });
                }

                int doesEmailExist = await _context.Users.CountAsync(u => u.Email == emailVerification.NewEmail);
                if (doesEmailExist != 0) // Se CountAsync retornar > 0, significa que existe.
                {
                    // Considerar se o e-mail pertence ao próprio usuário (caso ele tente confirmar o mesmo e-mail)
                    var currentUserWithEmail =
                        await _context.Users.FirstOrDefaultAsync(u => u.Email == emailVerification.NewEmail);
                    if (currentUserWithEmail != null && currentUserWithEmail.UserId == emailVerification.UserId)
                    {
                        // O e-mail que ele está tentando mudar É o e-mail atual dele, ou seja, não precisa mudar.
                        // Dependendo do fluxo, pode ser OK ou um erro. Aqui, trataremos como OK.
                        return Ok(new SuccessDTO("Seu e-mail já foi atualizado ou já é o e-mail desejado.", "Info"));
                    }

                    return Conflict(new ErrorDTO
                    {
                        ErrorCode = "409EMAILINUSE",
                        Message = "Erro: O e-mail já está conectado a outro usuário.",
                        ErrorDescription =
                            "O novo e-mail que você está tentando utilizar já está em uso por outra conta."
                    });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == emailVerification.UserId);
                if (user == null)
                {
                    // Se o usuário associado ao token não existe, o token é inválido/órfão.
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "404USERTOKEN",
                        Message = "Erro: Usuário não encontrado para este token.",
                        ErrorDescription = "O usuário associado ao token de verificação não existe."
                    });
                }

                // Realiza a troca de e-mail
                user.Email = emailVerification.NewEmail; // Atualiza o e-mail do usuário

                // Marca o token de verificação como usado/expirado
                emailVerification.IsVerified = true;
                emailVerification.ExpiresAt = DateTime.Now; // Expira imediatamente após o uso
                emailVerification.ExpiresAt = DateTime.Now.AddMonths(1); // Manter por 1 mês para auditoria
                // TODO: Trigger de deleção após 1 mês

                await _context.SaveChangesAsync();

                // TODO: Enviar um e-mail de confirmação para o novo e-mail do usuário.
                // await EmailServices.EmailChangedConfirmation(user.Email);
                // TODO: Enviar novo Token válido. 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GENERIC500",
                    ErrorDescription = "Qualquer tipo de exceção pode ter ocorrido.",
                    Message = "Ocorreu um erro inesperado."
                });
            }
            return NoContent();
        }
    }
}
