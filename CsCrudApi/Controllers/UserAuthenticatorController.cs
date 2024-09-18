using CsCrudApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CsCrudApi.Models.User;

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
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
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
                TpColor = model.TpColor
            };

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
    }
}
