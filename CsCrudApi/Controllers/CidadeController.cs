using Microsoft.AspNetCore.Mvc;
using CsCrudApi.Models;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CidadeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public CidadeController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("listar-cidades")]
        public async Task<ActionResult<dynamic>> ListCities()
        {
            return _context.Cidades.ToList();
        }
    }
}
