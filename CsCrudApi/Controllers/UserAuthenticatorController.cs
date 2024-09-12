using CsCrudApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserAuthenticatorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public UserAuthenticatorController(ApplicationDbContext context) 
        {
            _context = context;
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                // Executa uma consulta simples para verificar a conexão
                var users = await _context.Users.ToListAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                // Retorna uma mensagem de erro caso a conexão falhe
                return StatusCode(500, new { Message = "Erro ao conectar com o banco de dados", Error = ex.Message });
            }
        }
    }
}
