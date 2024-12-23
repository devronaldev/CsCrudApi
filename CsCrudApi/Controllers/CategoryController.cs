using CsCrudApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public CategoryController(ApplicationDbContext context) => _context = context;

        [HttpGet("listar-categorias")]
        public async Task<ActionResult<dynamic>> GetCategories() => await _context.Categories.ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetCategory([FromRoute] int id)
        {
            var c = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (c == null)
            {
                return NotFound(new
                {
                    Message = "Categoria não encontrada."
                });
            }
            return Ok(c);
        }
    }
}
