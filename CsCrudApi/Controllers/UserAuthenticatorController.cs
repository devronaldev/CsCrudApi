﻿using CsCrudApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CsCrudApi.Models.UserRelated;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using CsCrudApi.Services;
using CsCrudApi.Models.UserRelated.Request;
using System.Security.Claims;
using System.Text.RegularExpressions;

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
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    ModelState,
                    message = "Modelagem inválida. Requisição inválida."
                });
            }
            if (string.IsNullOrEmpty(model.Email))
            {
                return BadRequest(new
                {
                    message = "E-mail vazio ou nulo."
                });
            }
            if (string.IsNullOrEmpty(model.Password))
            {
                return Unauthorized(new
                {
                    message = "Senha vazia ou nula."
                });
            }
            if (model.Email.Length < 17)
            {
                return BadRequest(new
                {
                    message = "E-mail deve conter pelo menos 17 caracteres."
                });
            }
            var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*(),.?"":{}|<>])(?=.*[^a-zA-Z\d]).{8,}$");
            if (!regex.IsMatch(model.Password))
            {
                return Unauthorized(new
                {
                    message = "A senha não contém os padrões básicos de segurança."
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null) 
            {
                return NotFound(new
                {
                    message = "Usuário ou senha inexistente."
                });
            }
            if (user.IsEmailVerified == false)
            {
                return BadRequest(new 
                { 
                    message = "E-mail não verificado." 
                });
            }
            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password)) 
            {
                return Unauthorized(new
                {
                    message = "Usuário ou senha inexistente."
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

            model.Email = model.Email.Trim().ToLower();

            var verification = await IsEmailExistent(model.Email);
            if (verification.Result is ConflictObjectResult)
            {
                return Conflict(new { message = "O e-mail informado já está cadastrado." });
            }
            if (string.IsNullOrEmpty(model.Email))
            {
                return BadRequest(new
                {
                    message = "E-mail vazio ou nulo."
                });
            }
            if (model.Email.Length < 17)
            {
                return StatusCode(406, new
                {
                    message = "E-mail precisa ter mais de 17 caracteres",
                });
            }
            if (string.IsNullOrEmpty(model.Password))
            {
                return Unauthorized(new
                {
                    message = "Senha vazia ou nula."
                });
            }
            var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*(),.?"":{}|<>])(?=.*[^a-zA-Z\d]).{8,}$");
            if (!regex.IsMatch(model.Password))
            {
                return Unauthorized(new
                {
                    message = "A senha não contém os padrões básicos de segurança."
                });
            }

            // Se incorreto define o valor do enum para o default
            if (!Enum.IsDefined(typeof(ETipoInteresse), model.TipoInteresse))
            {
                model.TipoInteresse = ETipoInteresse.Orientado; 
            }
            if (!Enum.IsDefined(typeof(EGrauEscolaridade), model.GrauEscolaridade))
            {
                model.GrauEscolaridade = EGrauEscolaridade.Graduacao;
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
                Email = model.Email,
                Password = hashedPassword,
                CdCampus = model.CdCampus,
                Name = model.Name.Trim(),
                DtNasc = model.DtNasc,
                TipoInteresse = model.TipoInteresse,
                GrauEscolaridade = model.GrauEscolaridade,
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
