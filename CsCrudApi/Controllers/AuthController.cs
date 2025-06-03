using CsCrudApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CsCrudApi.Models.UserRelated;
using CsCrudApi.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;
using CsCrudApi.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Realiza o loginRequest de um usuário a partir do e-mail e senha fornecidos.
        /// </summary>
        /// <param name="loginRequest">Objeto contendo o e-mail e a senha do usuário.</param>
        /// <returns>Token JWT como string no corpo da resposta, caso o loginRequest seja bem-sucedido.</returns>
        /// <remarks>
        /// O token JWT retornado deve ser utilizado no cabeçalho `Authorization` das requisições futuras:
        /// Authorization: Bearer {seu_token}
        /// </remarks>
        /// <response code="200">Login bem-sucedido. Retorna o objeto LoginResponseDTO contendo o Access Token JWT (com validade de duas horas) e o Refresh Token.</response>
        /// <response code="400">Requisição inválida. Retorna um ErrorDTO com mais informações e uma mensagem amigável.</response>
        /// <response code="401">Senha inválida ou fora dos padrões. Retorna um ErrorDTO com mais informações e uma mensagem amigável.</response>
        /// <response code="404">O usuário não existe. Retorna um ErrorDTO com mais informações e uma mensagem amigável.</response>
        /// <response code="500">Erro interno no servidor.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] LoginRequestDTO loginRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorDTO{
                    Message = "Houve um erro no envio das informações.",
                    ErrorCode = "BR400MODEL",
                    ErrorDescription = "Modelagem inválida. Requisição inválida."
                });
            }
            if (string.IsNullOrEmpty(loginRequest.Email))
            {
                return BadRequest(new ErrorDTO
                {
                    Message = "E-mail vazio ou nulo.",
                    ErrorCode = "BR400EMAIL",
                    ErrorDescription = "A requisição tem o valor de e-mail vazio ou nulo."
                });
            }
            if (string.IsNullOrEmpty(loginRequest.Password))
            {
                return Unauthorized(new ErrorDTO
                {
                    Message = "Senha vazia ou nula.",
                    ErrorCode =  "UN401PASSWORD",
                    ErrorDescription = "A requisição tem o valor de senha vazio ou nulo."
                });
            }
            if (loginRequest.Email.Length < 17)
            {
                return BadRequest(new ErrorDTO
                {
                    Message = "E-mail tem tamanho insuficiente",
                    ErrorCode = "BR400EMAIL",
                    ErrorDescription = $"O e-mail tem apenas {loginRequest.Email.Length} caracteres. São necessários 17 caracteres ou mais."
                });
            }
            var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*(),.?"":{}|<>])(?=.*[^a-zA-Z\d]).{8,}$");
            if (!regex.IsMatch(loginRequest.Password))
            {
                return Unauthorized(new ErrorDTO
                {
                    Message = "A senha precisa conter ao menos 8 caracteres, caixa alta, caixa baixa, caractere especial e um número.",
                    ErrorCode = "BR400PASSWORD",
                    ErrorDescription = "A senha não é compatível com os padrões de segurança."
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginRequest.Email);
            if (user == null) 
            {
                return NotFound(new ErrorDTO
                {
                    Message = "Usuário inexistente.",
                    ErrorCode = "NOTF404",
                    ErrorDescription = "O usuário não está devidamente cadastrado em nosso banco de dados."
                });
            }
            if (user.IsEmailVerified == false)
            {
                return BadRequest(new ErrorDTO
                {
                    Message = "E-mail não verificado.",
                    ErrorCode = "BR400EMAIL",
                    ErrorDescription = "O e-mail não foi corretamente verificado."
                });
            }
            if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password)) 
            {
                return Unauthorized(new ErrorDTO
                {
                    Message = "E-mail ou senha incorretos.",
                    ErrorCode = "BR401PASSWORD",
                    ErrorDescription = "A senha não foi devidamente verificada."
                });
            }
            
            var token = TokenServices.GenerateToken(user);
            var refreshToken = TokenServices.GenerateRefreshToken(user);
            
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax, 
                Expires = refreshToken.ExpiresAt,
                // Path = "/api/Auth/refresh",
                // Domain = "yourdomain.com", Configurar futuramente.
            };
            Response.Cookies.Append("RefreshToken", refreshToken.TokenHash, cookieOptions);
            
            refreshToken.TokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken.TokenHash);
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            
            var loginResponseDTO = new LoginResponseDTO
            {
                AccessToken = token,
                AccessExpiresAt = DateTime.Now.AddHours(2),
                RefreshExpiresAt = refreshToken.ExpiresAt,
                TokenType = "Bearer"
            };
            
            return Ok(loginResponseDTO);
        }
        
        /// <summary>
        /// Realiza o logout do usuário, invalidando todos os seus Refresh Tokens ativos.
        /// </summary>
        /// <returns>Um ActionResult indicando o sucesso da operação.</returns>
        /// <response code="204">Logout bem-sucedido. Todos os Refresh Tokens do usuário foram invalidados.</response>
        /// <response code="401">A requisição não foi autorizada (token JWT inválido ou ausente).</response>
        /// <response code="500">Ocorreu um erro interno inesperado no servidor ao tentar processar o logout.</response>
        [HttpPost("logout")]
        [RequireHttps]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Logout()
        {
            ClaimsPrincipal claimsPrincipal = HttpContext.User;
            try
            {
                var userEmail =  TokenServices.GetTokenEmailAsync(claimsPrincipal);
                var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    return Unauthorized(new ErrorDTO
                    {
                        ErrorCode = "AUTH401TOKENINVALID_JWT",
                        ErrorDescription = "Token JWT inválido (assinatura ou formato) ou usuário não encontrado.",
                        Message = "Sessão inválida."
                    });
                }
                await RevokeAllRefreshTokensForUser(user.UserId);

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no logout: {ex.Message} - {ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDTO
                {
                    ErrorCode = "SERVER500_LOGOUT",
                    ErrorDescription = $"Erro inesperado ao processar o logout.",
                    Message = "Não foi possível realizar o logout. Por favor, tente novamente."
                });
            }
        }

        /// <summary>
        /// Renova o Access Token e o Refresh Token.
        /// </summary>
        /// <returns>Um novo Access Token e metadados de expiração.</returns>
        /// <response code="200">Tokens renovados com sucesso.</response>
        /// <response code="401">Refresh Token ausente, inválido, expirado ou revogado. Requer novo login.</response>
        /// <response code="500">Ocorreu um erro interno inesperado no servidor.</response>
        /// [RequireHttp]
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LoginResponseDTO>> RefreshToken()
        {
            var refreshTokenJwt = HttpContext.Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(refreshTokenJwt))
            {
                return Unauthorized(new ErrorDTO
                {
                    ErrorCode = "AUTH401REFRESHTOKENMISSING",
                    ErrorDescription = "Refresh Token não encontrado nos cookies da requisição.",
                    Message = "Sessão expirada. Faça login novamente."
                });
            }

            try
            {
                ClaimsPrincipal? claimsPrincipal = TokenServices.ValidateJwtToken(refreshTokenJwt);
                if (claimsPrincipal == null)
                {
                    return Unauthorized(new ErrorDTO
                    {
                        ErrorCode = "AUTH401REFRESHTOKENINVALID_JWT",
                        ErrorDescription = "Refresh Token JWT inválido (assinatura ou formato).",
                        Message = "Sessão inválida. Faça login novamente."
                    });
                }

                var userEmail =  TokenServices.GetTokenEmailAsync(claimsPrincipal);
                var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    return Unauthorized(new ErrorDTO
                    {
                        ErrorCode = "AUTH401USERNOTFOUND_REFRESH",
                        ErrorDescription = "Usuário associado ao Refresh Token não encontrado no banco de dados.",
                        Message = "Sessão inválida. Faça login novamente."
                    });
                }

                var storedRefreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt =>
                        rt.UserId == user.UserId && rt.IsRevoked == false &&
                        TokenServices.VerifyTokenHash(refreshTokenJwt, rt.TokenHash));

                if (storedRefreshToken == null)
                {
                    await RevokeAllRefreshTokensForUser(user.UserId); 
                    return Unauthorized(new ErrorDTO
                    {
                        ErrorCode = "AUTH401REFRESHTOKEN_NOTFOUND_OR_REUSED",
                        ErrorDescription =
                            "Refresh Token não encontrado, já usado ou revogado. Todas as sessões foram encerradas.",
                        Message = "Sessão inválida ou comprometida. Faça login novamente."
                    });
                }

                if (storedRefreshToken.ExpiresAt < DateTime.UtcNow)
                {
                    storedRefreshToken.IsRevoked = true;
                    await _context.SaveChangesAsync();
                    return Unauthorized(new ErrorDTO
                    {
                        ErrorCode = "AUTH401REFRESHTOKEN_EXPIRED_DB",
                        ErrorDescription = "Refresh Token expirado no banco de dados.",
                        Message = "Sessão expirada. Faça login novamente."
                    });
                }

                storedRefreshToken.IsRevoked = true;
                
                var newAccessTokenJwt = TokenServices.GenerateToken(user);
                var newAccessExpiresAt = DateTime.UtcNow.AddHours(2);

                var newRefreshTokenJwt = TokenServices.GenerateToken(user, TokenServices.ETokenType.Refresh);
                var newRefreshExpiresAt = DateTime.UtcNow.AddDays(30);

                var newRefreshTokenEntity = new RefreshToken
                {
                    Id = Guid.NewGuid().ToString("N"), 
                    UserId = user.UserId,
                    TokenHash = TokenServices.HashToken(newRefreshTokenJwt), 
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = newRefreshExpiresAt,
                    IsRevoked = false,
                    ReplacedByTokenId = null 
                };

                storedRefreshToken.ReplacedByTokenId = newRefreshTokenEntity.Id;

                _context.RefreshTokens.Add(newRefreshTokenEntity);
                _context.RefreshTokens.Update(storedRefreshToken); 

                await _context.SaveChangesAsync();

               var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = newRefreshExpiresAt,
                    // Path = "/api/Auth/refresh",
                    // Domain = "yourdomain.com", // Configurar futuramente.
                };
                Response.Cookies.Append("RefreshToken", newRefreshTokenJwt, cookieOptions);

                var loginResponseDTO = new LoginResponseDTO
                {
                    AccessToken = newAccessTokenJwt,
                    AccessExpiresAt = newAccessExpiresAt,
                    RefreshExpiresAt = newRefreshExpiresAt,
                    TokenType = "Bearer"
                };

                return Ok(loginResponseDTO);
            }
            catch (SecurityTokenExpiredException) 
            {
                return Unauthorized(new ErrorDTO
                {
                    ErrorCode = "AUTH401REFRESHTOKENEXPIRED_JWT",
                    ErrorDescription = "Refresh Token JWT expirado.",
                    Message = "Sessão expirada. Faça login novamente."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado ao renovar token: {ex.Message} - {ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDTO
                {
                    Message = "Erro desconhecido ao gerar token de acesso.",
                    ErrorCode = "SERVER500",
                    ErrorDescription = ex.Message
                });
            }
        }

        /// <summary>
        /// Cria um novo usuário com base nos dados fornecidos no DTO e envia um e-mail para verificação de conta.
        /// </summary>
        /// <param name="model">DTO contendo as informações para cadastro de usuário.</param>
        /// <returns>Mensagem de confirmação e ID do usuário criado.</returns>
        /// <response code="200">A criação foi bem sucedida. Retorna uma string com a mensagem de confirmação.</response>
        /// <response code="400">Requisição inválida. Retorna um ErrorDTO com mais informações e uma mensagem amigável.</response>
        /// <response code="409">O e-mail informado já é utilizado no nosso banco de dados.</response>
        /// <response code="500">Erro interno no servidor.</response>
        [HttpPost("cadastrar")]
        [ProducesResponseType(typeof(SuccessDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status409Conflict)]
        [AllowAnonymous]
        public async Task<ActionResult<string>> Register([FromBody] UserSignUpDTO model)
        {
            
            if (model == null)
            {
                return BadRequest(new ErrorDTO
                {
                    Message = "Os dados do usuário são obrigatórios.",
                    ErrorCode = "BR400MODEL",
                    ErrorDescription = "O Model foi enviado com valores nulos ou vazio."
                });
            }
            
            // Verificar nome
            if (string.IsNullOrEmpty(model.Name.Trim()))
            {
                return BadRequest(new ErrorDTO
                {
                    Message = "O nome do usuário está vazio.",
                    ErrorCode = "BR400NAME",
                    ErrorDescription = "A propriedade 'model.Name' está nula ou vazio."
                });
            }

            // Verificar se o e-mail já está cadastrado
            var verification = await IsEmailExistent(model.Email);
            if (verification.Result is ConflictObjectResult)
            {
                return Conflict(new ErrorDTO
                {
                    Message = "O e-mail informado já está cadastrado.",
                    ErrorCode = "BR400EMAIL",
                    ErrorDescription = "O e-mail foi encontrado em um outro cadastro ou alteração de e-mail."
                });
            }

            // Validar senha
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*(),.?""{}|<>]).{8,}$");
            if (string.IsNullOrEmpty(model.Password) || !passwordRegex.IsMatch(model.Password))
            {
                return BadRequest(new ErrorDTO
                {
                    Message = "A senha precisa conter ao menos 8 caracteres, caixa alta, caixa baixa, caractere especial e um número.",
                    ErrorCode = "BR400PASSWORD",
                    ErrorDescription = "A senha não é compatível com os padrões de segurança."
                });
            }
            
            var user = (User)model;
            // Validar e-mail
            user.Email = user.Email?.Trim().ToLower();
            if (string.IsNullOrEmpty(user.Email) || user.Email.Length < 17)
            {
                return BadRequest(new ErrorDTO
                {
                    Message = "E-mail tem tamanho insuficiente",
                    ErrorCode = "BR400EMAIL",
                    ErrorDescription = $"O e-mail tem caracteres insuficientes. São necessários 17 caracteres ou mais."
                });
            }
            
            // Validar enums e atribuir valores padrão
            user.TipoInteresse = Enum.TryParse(user.TipoInteresse.ToString(), out ETipoInteresse interesse)
                ? interesse
                : ETipoInteresse.Orientado;
            user.GrauEscolaridade = Enum.TryParse(user.GrauEscolaridade.ToString(), out EGrauEscolaridade escolaridade)
                ? escolaridade
                : EGrauEscolaridade.Graduacao;
            user.TpColor = Enum.TryParse(user.TpColor.ToString(), out EColor color)
                ? color
                : EColor.White;

            // Configurar dados do usuário
            user.NmSocial ??= model.Name?.Trim();
            user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

            try
            {
                // Salvar usuário no banco de dados
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Enviar e-mail de verificação
                await EmailServices.SendVerificationEmail(user);

                return Ok(new SuccessDTO("Usuário registrado com sucesso!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, 
                    new ErrorDTO
                    {
                        Message = "Erro desconhecido ao registrar usuário.",
                        ErrorCode = "SERVER500",
                        ErrorDescription = ex.Message
                    });
            }
        }

        /// <summary>
        /// Verifica se o e-mail informado já está em uso.
        /// </summary>
        /// <param name="email">E-mail a ser verificado.</param>
        /// <returns>
        /// Retorna:
        /// <list type="bullet">
        /// <item><description><see cref="ConflictResult"/> se o e-mail já estiver em uso ou em processo de verificação.</description></item>
        /// <item><description><see cref="NotFoundResult"/> se o e-mail estiver disponível.</description></item>
        /// <item><description><see cref="BadRequestResult"/> se o e-mail for nulo ou vazio.</description></item>
        /// <item><description><see cref="StatusCodeResult"/> 406 se o e-mail for muito curto.</description></item>
        /// </list>
        /// </returns>
        /// <response code="409">E-mail já cadastrado ou em processo de troca.</response>
        /// <response code="404">E-mail disponível para cadastro.</response>
        /// <response code="400">E-mail não pode ser vazio.</response>
        /// <response code="406">E-mail precisa ter mais de 17 caracteres.</response>
        [HttpGet("email-existe")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status406NotAcceptable)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(SuccessDTO), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SuccessDTO>> IsEmailExistent(string email)
        {
            email = email.ToLower().Trim();
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400EMAIL",
                    Message = "O e-mail não pode ser vazio.",
                    ErrorDescription = "O valor de e-mail foi verificado com nulo ou vazio"
                });
            }
            if (email.Length < 17)
            {
                return StatusCode(406, new ErrorDTO
                {
                    ErrorCode = "NOTACP406EMAIL",
                    Message = "E-mail tem menos de 17 caracteres.",
                    ErrorDescription = $"O e-mail:{email} tem apenas {email.Length} caracteres."
                });
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                return Conflict(new ErrorDTO
                {
                    ErrorCode = "CF409EXIST",
                    ErrorDescription = "O e-mail foi encontrado como cadastrado dentro do nosso banco de dados.",
                    Message = "E-mail já cadastrado."
                });
            }

            // Verifica se o e-mail está envolvido em alguma troca (Pode ser tanto um e-mail novo, quanto um antigo de uma solicitação).
            var trocaEmail = await _context.EmailVerifications.FirstOrDefaultAsync(t => t.NewEmail == email);
            if (trocaEmail != null) { 
                return Conflict(new ErrorDTO
                {
                    ErrorCode = "CF409EXIST",
                    ErrorDescription = "O e-mail foi encontrado como cadastrado dentro do nosso banco de dados.",
                    Message = "E-mail já cadastrado."
                });
            }

            return NotFound(new SuccessDTO("O e-mail não foi encontrado."));
        }

        /// <summary>
        /// Verifica um e-mail com base no token JWT fornecido.
        /// </summary>
        /// <param name="token">Token JWT de verificação de e-mail.</param>
        /// <returns>
        /// Retorna:
        /// <list type="bullet">
        /// <item><description><see cref="OkResult"/> se o e-mail for verificado com sucesso.</description></item>
        /// <item><description><see cref="BadRequestResult"/> se o token for inválido, claim ausente ou usuário não encontrado.</description></item>
        /// </list>
        /// </returns>
        /// <response code="200">E-mail verificado com sucesso.</response>
        /// <response code="400">Token inválido, claim ausente ou usuário não encontrado.</response>
        /// <response code="404">O usuário não foi encontrado.</response>
        [HttpGet("verificar-email")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SuccessDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SuccessDTO>> VerifyEmail(string token)
        {
           try
           {
               var userEmail =  TokenServices.GetTokenEmailAsync(TokenServices.ValidateJwtToken(token));
               var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "NOTF404USER",
                        Message = "Usuário não encontrado.",
                        ErrorDescription = "Não foi encontrado usuário com o e-mail informado."
                    }); // TODO: Possibilitar retorno de CSHTML
                }

                user.IsEmailVerified = true;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new SuccessDTO("E-mail verificado com sucesso!"));
           }
           catch (Exception ex)
           {
               return BadRequest(new ErrorDTO
               {
                   ErrorCode = "SERVER500",
                   ErrorDescription = ex.Message,
                   Message = $"Token inválido. Por favor, realizar novamente o login."
               });
           }
        }

        /// <summary>
        /// Cancela o pré-cadastro de um usuário com base no e-mail, caso ele ainda não tenha sido verificado.
        /// </summary>
        /// <param name="email">E-mail do usuário a ser removido.</param>
        /// <returns>
        /// Retorna:
        /// <list type="bullet">
        /// <item><description><see cref="OkObjectResult"/> caso o cadastro seja removido com sucesso.</description></item>
        /// <item><description><see cref="NotFoundResult"/> se o e-mail não estiver cadastrado.</description></item>
        /// <item><description><see cref="BadRequestObjectResult"/> se o e-mail já estiver verificado ou ocorrer um erro ao excluir.</description></item>
        /// </list>
        /// </returns>
        /// <response code="200">Cadastro removido com sucesso.</response>
        /// <response code="400">O e-mail já foi verificado ou ocorreu um erro durante a exclusão.</response>
        /// <response code="404">E-mail não encontrado.</response>
        [HttpGet("cancelar-cadastro")]
        [ProducesResponseType(typeof(SuccessDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SuccessDTO>> DeleteRegister(string token)
        {
            var claimsPrincipal = TokenServices.ValidateJwtToken(token);
            if (claimsPrincipal == null)
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400TOKEN",
                    Message = "Erro: Token inválido ou não pode ser validado.",
                    ErrorDescription = "O Token não apresenta 'Claims' válidas após ser validado."
                }); // TODO: Possibilitar retorno de CSHTML
                    
            }

            var emailClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimValueTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400TOKEN",
                    Message = "Erro: Token inválido ou não pode ser validado.",
                    ErrorDescription = "Não foi encontrado uma claim de e-mail."
                }); // TODO: Possibilitar retorno de CSHTML
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == emailClaim);
            if (user == null)
            {
                return NotFound(new ErrorDTO
                {
                    ErrorCode = "NOTF404USER",
                    Message = "Usuário não encontrado.",
                    ErrorDescription = "Não foi encontrado usuário com o e-mail informado."
                }); // TODO: Possibilitar retorno de CSHTML
            }

            if (user.IsEmailVerified)
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400VERIFIED",
                    Message = "E-mail já verificado. Impossível de excluir pré-cadastro. Caso tenha interesse, por favor, logar na página e ir até Configurações > Conta e Segurança e clicar no botão excluir perfil.",
                    ErrorDescription = "Após a verificação, nenhuma conta pode ser apagada por esse endpoint."
                });
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Ok(new SuccessDTO("Cadastro removido com sucesso."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "SERVER500",
                    ErrorDescription = ex.Message,
                    Message = $"Erro na exclusão de registro."
                });
            }
        }

        [NonAction]
        private async Task RevokeAllRefreshTokensForUser(int userId)
        {
            var tokensToRevoke = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in tokensToRevoke)
            {
                token.IsRevoked = true;
                _context.RefreshTokens.Update(token);
            }
            await _context.SaveChangesAsync();
        }
    }
}
