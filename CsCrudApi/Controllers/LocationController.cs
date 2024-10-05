using Microsoft.AspNetCore.Mvc;
using CsCrudApi.Models;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public LocationController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("listar-cidades")]
        public async Task<ActionResult<dynamic>> ListCities() => _context.Cidades.ToList();

        [HttpPost("listar-campi")]
        public async Task<ActionResult<dynamic>> ListCampi() => _context.Campi.ToList();
    }
}
