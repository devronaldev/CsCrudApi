using CsCrudApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CsCrudApi.Models.User;
using Microsoft.IdentityModel.Tokens;
using Services;
using System.IdentityModel.Tokens.Jwt;
using CsCrudApi.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public UserAuthController(ApplicationDbContext context, IConfiguration configuration) 
        {
            _context = context;
            _configuration = configuration;
        }

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
            /*if (user.IsEmailVerified == false)
            {
                return BadRequest(new { message = "E-mail não verificado." });
            }*/

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
                user,
                token, 
            };
        }
        
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<dynamic>> Register([FromBody] User model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "O modelo do usuário não pode ser nulo." });
            }

            // Verificar se o e-mail já existe
            var verification = await IsEmailExistent(model.Email.ToLower().Trim());
            if (verification.Result is ConflictObjectResult)
            {
                return Conflict(new { message = "O e-mail informado já está cadastrado." });
            };


            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            
            //IMPLEMENTAR SUBSTITUIÇÃO DE NOME SOCIAL POR NOME!!!
            if (model.NmSocial == "" || model.NmSocial == null)
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
                IsEmailVerified = model.IsEmailVerified
            };

            await EmailServices.SendVerificationEmail(user);

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Usuário registrado com sucesso!" });
            }
            catch (Exception ex)
            {
                // Tratar exceções futuramente
                return StatusCode(500, new { message = "Erro ao registrar usuário", error = ex.Message });
            }
        }

        [HttpPost("email-existe")]
        public async Task<ActionResult<dynamic>> IsEmailExistent(string email)
        {
            email.ToLower().Trim();
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "O e-mail não pode ser vazio." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                return Conflict(new { message = "E-mail já cadastrado." });
            }

            return NotFound(new { message = "O e-mail não foi encontrado." });
        }


        [HttpGet("verificar-email")]
        public async Task<ActionResult<dynamic>> VerifyEmail(string token)
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

                var jwtToken = (JwtSecurityToken)validatedToken;
                var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

                if (emailClaim == null)
                {
                    return BadRequest("Token inválido.");
                }

                var userEmail = emailClaim.Value;
                var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                {
                    return BadRequest("Usuário não encontrado.");
                }

                user.IsEmailVerified = true; // Atualiza o campo no modelo
                _context.Users.Update(user); // Atualiza o modelo no contexto em código
                await _context.SaveChangesAsync(); // Passa o update para o Banco

                return Ok("E-mail verificado com sucesso!");
            }
            catch
            {
                return BadRequest("Token inválido.");
            }
        }

        [HttpPost("checkdeploy")]
        public async Task<ActionResult<dynamic>> CheckDeploy([FromBody] LoginDTO user)
        {
            return new { user };
        }
    }
}
