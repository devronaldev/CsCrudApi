using Microsoft.AspNetCore.Mvc;
using CsCrudApi.Models;
using Microsoft.AspNetCore.Authorization;
using CsCrudApi.Models.UserRelated;
using Microsoft.EntityFrameworkCore;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public LocationController(ApplicationDbContext context, IConfiguration configuration) => _context = context;

        [HttpGet("listar-cidades")]
        [AllowAnonymous]
        public async Task<ActionResult<dynamic>> ListCities() => _context.Cidades.ToList();

        [AllowAnonymous]
        [HttpGet("listar-campi")]
        public async Task<ActionResult<dynamic>> ListCampi() => _context.Campi.ToList();

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
    }
}
