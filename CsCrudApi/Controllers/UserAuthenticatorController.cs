using CsCrudApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CsCrudApi.Models.UserRelated;
using System.IdentityModel.Tokens.Jwt;
using CsCrudApi.Services;
using CsCrudApi.Models.UserRelated.Request;
using System.Security.Claims;
using System.Text.RegularExpressions;
using CsCrudApi.DTOs;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public UserAuthController(ApplicationDbContext context, FileServices fileServices)
        {
            _context = context;
        }
        
        
        /// <summary>
        /// Realizar o login após receber o e-mail e senha
        /// </summary>
        /// <param name="login">Objeto contendo e-mail e senha do usuario</param>
        /// <returns>Um ‘token’ JWT caso o ‘login’ seja bem-sucedido.</returns>
        /// <response code="200">Retorna o ‘token’ JWT com validade de duas horas</response>
        /// <response code="400">Requisição inválida. Retorna um DTO com mais informações e uma mensagem amigável para ser exibida.</response>
        /// <response code="401">Senha inválida ou fora dos padrões. Retorna um DTO com mais informações e uma mensagem amigável para ser exibida.</response>
        /// <response code="404">O usuário não existe. Retorna um DTO com mais informações e uma mensagem amigável para ser exibida.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        [AllowAnonymous]
        public async Task<ActionResult<string>> Login([FromBody] LoginDTO login)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorDTO{
                    Message = "Houve um erro no envio das informações.",
                    ErrorCode = "BR400MODEL",
                    ErrorDescription = "Modelagem inválida. Requisição inválida."
                });
            }
            if (string.IsNullOrEmpty(login.Email))
            {
                return BadRequest(new ErrorDTO
                {
                    Message = "E-mail vazio ou nulo.",
                    ErrorCode = "BR400EMAIL",
                    ErrorDescription = "A requisição tem o valor de e-mail vazio ou nulo."
                });
            }
            if (string.IsNullOrEmpty(login.Password))
            {
                return Unauthorized(new ErrorDTO
                {
                    Message = "Senha vazia ou nula.",
                    ErrorCode =  "UN401PASSWORD",
                    ErrorDescription = "A requisição tem o valor de senha vazio ou nulo."
                });
            }
            if (login.Email.Length < 17)
            {
                return BadRequest(new ErrorDTO
                {
                    Message = "E-mail tem tamanho insuficiente",
                    ErrorCode = "BR400EMAIL",
                    ErrorDescription = $"O e-mail tem apenas {login.Email.Length} caracteres. São necessários 17 caracteres ou mais."
                });
            }
            var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*(),.?"":{}|<>])(?=.*[^a-zA-Z\d]).{8,}$");
            if (!regex.IsMatch(login.Password))
            {
                return BadRequest(new ErrorDTO
                {
                    Message = "A senha precisa conter ao menos 8 caracteres, caixa alta, caixa baixa, caractere especial e um número.",
                    ErrorCode = "BR400PASSWORD",
                    ErrorDescription = "A senha não é compatível com os padrões de segurança."
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == login.Email);
            if (user == null) 
            {
                return BadRequest(new ErrorDTO
                {
                    Message = "Usuário inexistente.",
                    ErrorCode = "BR400NULL",
                    ErrorDescription = "O usuário não está devidamente cadastro em nosso banco de dados."
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
            if (!BCrypt.Net.BCrypt.Verify(login.Password, user.Password)) 
            {
                return Unauthorized(new
                {
                    Message = "E-mail ou senha incorretos.",
                    ErrorCode = "BR401PASSWORD",
                    ErrorDescription = "A senha não foi devidamente verificada."
                });
            }
            var token = Services.TokenServices.GenerateToken(user);
            user.Password = "";
            return token;
        }

        /// <summary>
        /// Esse endpoint é responsável por cadastrar novos funcionários e ao fim realizar o envio de um e-mail de verificação.
        /// </summary>
        /// <param name="model">É necessário um DTO de SignUp</param>
        /// <returns>Retorna uma string informando que o e-mail foi devidamente cadastrado.</returns>
        [HttpPost("cadastrar")]
        [AllowAnonymous]
        public async Task<ActionResult<string>> Register([FromBody] UserSignUpDTO model)
        {
            
            if (model == null)
            {
                return BadRequest(new { message = "Os dados do usuário são obrigatórios." });
            }
            
            // Verificar nome
            if (string.IsNullOrEmpty(model.Name.Trim()))
            {
                return BadRequest(new
                {
                    message = "O nome não pode estar vazio."
                });
            }

            // Verificar se o e-mail já está cadastrado
            var verification = await IsEmailExistent(model.Email);
            if (verification.Result is ConflictObjectResult)
            {
                return Conflict(new { message = "O e-mail informado já está cadastrado." });
            }

            // Validar senha
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*(),.?""{}|<>]).{8,}$");
            if (string.IsNullOrEmpty(model.Password) || !passwordRegex.IsMatch(model.Password))
            {
                return BadRequest(new { message = "A senha não atende aos requisitos mínimos de segurança." });
            }
            
            var user = (User)model;
            // Validar e-mail
            user.Email = user.Email?.Trim().ToLower();
            if (string.IsNullOrEmpty(model.Email) || model.Email.Length < 17)
            {
                return BadRequest(new { message = "E-mail inválido. Deve ter pelo menos 17 caracteres." });
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

                return Ok(new { message = "Usuário registrado com sucesso!", userId = user.UserId });
            }
            catch (Exception ex)
            {
                // Logar o erro e retornar mensagem amigável
                Console.Error.WriteLine($"Erro ao registrar usuário: {ex}");
                return StatusCode(500, new { message = "Erro ao registrar usuário.", error = ex.Message });
            }
        }


        [HttpGet("email-existe")]
        [AllowAnonymous]
        public async Task<ActionResult<dynamic>> IsEmailExistent(string email)
        {
            email = email.ToLower().Trim();
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "O e-mail não pode ser vazio." });
            }
            if (email.Length < 17)
            {
                return StatusCode(406, new
                {
                    message = "E-mail precisa ter mais de 17 caracteres",
                });
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                return Conflict(new { message = "E-mail já cadastrado." });
            }

            // Verifica se o e-mail está envolvido em alguma troca (Pode ser tanto um e-mail novo, quanto um antigo de uma solicitação).
            var trocaEmail = await _context.EmailVerifications.FirstOrDefaultAsync(t => t.NewEmail == email);
            if (trocaEmail != null) { 
                return Conflict(new { message = "E-mail já cadastrado." }); 
            }

            return NotFound(new { message = "O e-mail não foi encontrado." });
        }

        [HttpGet("verificar-email")]
        [AllowAnonymous]
        public async Task<ActionResult<dynamic>> VerifyEmail(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = TokenServices.GetKey();

            try
            {
                var claimsPrincipal = TokenServices.ValidateJwtToken(token);
                if (claimsPrincipal == null)
                {
                    return BadRequest("Erro: Token inválido ou não pode ser validado.");
                }

                var emailClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimValueTypes.Email)?.Value;
                if (string.IsNullOrEmpty(emailClaim))
                {
                    return BadRequest("Token inválido, claim de e-mail ausente.");
                }

                var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == emailClaim);
                if (user == null)
                {
                    return BadRequest("Usuário não encontrado.");
                }

                user.IsEmailVerified = true;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok("E-mail verificado com sucesso!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Token inválido. Erro: {ex.Message}");
            }
        }

        [HttpGet("cancelar-cadastro")]
        public async Task<ActionResult<dynamic>> DeleteRegister(string email)
        {
            email = email.ToLower().Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return NotFound(new
                {
                    Message = "E-mail não encontrado"
                });
            }

            if (user.IsEmailVerified)
            {
                return BadRequest(new
                {
                    Message = "E-mail já verificado. Impossível de excluir pré-cadastro. Caso tenha interesse, por favor, logar na página e ir até Configurações > Conta e Segurança e clicar no botão excluir perfil."
                });
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Ok("Cadastro removido com sucesso.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro na exclusão de registro: {ex.Message}");
            }
        }
    }
}
