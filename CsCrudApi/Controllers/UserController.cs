using CsCrudApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public UserController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("perfil")]
        public async Task<ActionResult<dynamic>> Profile([FromBody] int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == id);

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

            var cidadeCampus = await _context.Cidades.FirstOrDefaultAsync(c => c.IdCidade == campus.CdCidade);

            if (cidadeCampus == null)
            {
                return NotFound("Campus com cidade não cadastrada ou cadastrado incorretamente.");
            }
            
            return new 
            {
                user.IdUser,
                user.NmSocial,
                user.DtNasc,
                user.Email,
                user.TpPreferencia,
                user.DescTitulo,
                cidade,
                campus.Id,
                campus.SgCampus,
                campus.CampusName,
                cidadeCampus
            };
        }
    }
}
