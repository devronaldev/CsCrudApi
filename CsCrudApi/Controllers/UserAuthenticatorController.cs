using CsCrudApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CsCrudApi.Models.UserRelated;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using CsCrudApi.Services;
using CsCrudApi.Models.UserRelated.Request;
using System.Security.Claims;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public UserAuthController(ApplicationDbContext context) => _context = context;

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<dynamic>> Login([FromBody] LoginDTO model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);


            if (user == null) 
            {
                return NotFound(new
                {
                    message = "Usuário ou senha inexistente"
                });
            }

            //ADICIONAR DEPOIS DE RESOLVER EMAIL SHOOTER
            if (user.IsEmailVerified == false)
            {
                return BadRequest(new { message = "E-mail não verificado." });
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password)) 
            {
                return Unauthorized(new
                {
                    //Mais seguro se eu não informar que o problema é na senha
                    message = "Usuário ou senha inexistente"
                });
            }
            var token = Services.TokenServices.GenerateToken(user);
            user.Password = "";
            return new 
            {
                Usuario = user,
                token, 
            };
        }
        
        [HttpPost("cadastrar")]
        [AllowAnonymous]
        public async Task<ActionResult<dynamic>> Register([FromBody] User model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "O modelo do usuário não pode ser nulo." });
            }

            var verification = await IsEmailExistent(model.Email.ToLower().Trim());
            if (verification.Result is ConflictObjectResult)
            {
                return Conflict(new { message = "O e-mail informado já está cadastrado." });
            }

            // Se incorreto define o valor do enum para o default
            if (!Enum.IsDefined(typeof(EPreferencia), model.TpPreferencia))
            {
                model.TpPreferencia = EPreferencia.Produzir; 
            }
            if (!Enum.IsDefined(typeof(ETitulo), model.DescTitulo))
            {
                model.DescTitulo = ETitulo.Egresso;
            }
            if (!Enum.IsDefined(typeof(EColor), model.TpColor))
            {
                model.TpColor = EColor.White; 
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
            if (string.IsNullOrEmpty(model.NmSocial))
            {
                model.NmSocial = model.Name;
            }

            var user = new User
            {
                Email = model.Email.Trim().ToLower(),
                Password = hashedPassword,
                CdCampus = model.CdCampus,
                Name = model.Name.Trim(),
                DtNasc = model.DtNasc,
                TpPreferencia = model.TpPreferencia,
                DescTitulo = model.DescTitulo,
                NmSocial = model.NmSocial.Trim(),
                TpColor = model.TpColor,
                CdCidade = model.CdCidade,
                IsEmailVerified = false
            };

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                await EmailServices.SendVerificationEmail(user);
                return Ok(new { message = "Usuário registrado com sucesso!" });
            }
            catch (Exception ex)
            {
                //SE HOUVER ERRO SÓ DEUS SABE.
                return StatusCode(500, new { message = "Erro ao registrar usuário", error = ex.Message });
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

        //ATUALIZAR PARA TOKEN
        [HttpGet("cancelar-cadastro")]
        public async Task<ActionResult<dynamic>> DeleteRegister(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return NotFound("E-mail não encontrado");
            }

            if (user.IsEmailVerified)
            {
                return BadRequest("E-mail já verificado. Impossível de excluir pré-cadastro. Caso tenha interesse, por favor, logar na página e ir até Configurações > Conta e Segurança e clicar no botão excluir perfil.");

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

        /*
        [HttpPost("checkdeploy")]
        public async Task<ActionResult<dynamic>> CheckDeploy([FromBody] LoginDTO user)
        {
            return new { user };
        }
        */
    }
}
