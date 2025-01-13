using Microsoft.AspNetCore.Mvc;
using CsCrudApi.Models;
using Microsoft.AspNetCore.Authorization;
using CsCrudApi.Models.UserRelated;
using Microsoft.EntityFrameworkCore;
using CsCrudApi.Models.UserRelated.CollegeRelated;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public LocationController(ApplicationDbContext context) => _context = context;

        [HttpGet("listar-cidades")]
        [AllowAnonymous]
        public async Task<ActionResult<dynamic>> ListCities() => await _context.Cidades.ToListAsync();

        [AllowAnonymous]
        [HttpGet("listar-campi")]
        public async Task<ActionResult<dynamic>> ListCampi() => await _context.Campi.ToListAsync();

        [AllowAnonymous]
        [HttpGet("areas")]
        public async Task<ActionResult<dynamic>> ListAreas() => await _context.Areas.ToListAsync();

        [AllowAnonymous]
        [HttpGet("cidade/{id}")]
        public async Task<ActionResult<dynamic>> GetCity([FromRoute] int id)
        {
            if(id == 0)
            {
                return BadRequest(new
                {
                    message = "Valor inválido"
                });
            }

            var cidade = await _context.Cidades.FirstOrDefaultAsync(c => c.IdCidade == id);
            if (cidade == null)
            {
                return NotFound(new
                {
                    message = "Cidade não encontrada"
                });
            }

            return Ok(cidade);
        }

        [HttpGet("campus/{id}")]
        public async Task<Campus> GetCampus([FromRoute] int id)
        {
            return await _context.Campi.FirstOrDefaultAsync(c => c.Id == id);
        }

        [AllowAnonymous]
        [HttpGet("campus-por-cidade/{id}")]
        public async Task<ActionResult<dynamic>> GetCampiByCity([FromRoute] int id)
        {
            if (id == 0)
            {
                return BadRequest(new
                {
                    message = "Valor inválido"
                });
            }

            var list = await _context.Campi.Where(c=> c.CdCidade == id).ToListAsync();

            if (!list.Any())
            {
                return NotFound(new
                {
                    message = "Não foi encontrado nenhum campus nessa cidade."
                });
            }

            return Ok(list);
        }

        [HttpGet("alunos-por-campus/{campusId}")]
        public async Task<ActionResult<List<object>>> GetUserByCampi([FromRoute] int campusId)
        {
            if(campusId == 0)
            {
                return BadRequest(new {
                    Message = "O id não pode ser 0 ou vazio."
                });
            }

            var users = await _context.Users
            .Where(u => u.CdCampus == campusId && u.IsEmailVerified == true)
            .Select(u => new{
                u.UserId,
                u.NmSocial,
                u.CdCidade,
                u.CursoId,
                u.TipoInteresse
            })
            .ToListAsync();

            if(users == null)
            {
                return BadRequest(new {
                    Message = "Não foram encontrados alunos dessa instituição."
                });
            }

            return Ok(users);
        }
    }
}
