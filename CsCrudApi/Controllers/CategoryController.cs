using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.UserRelated.CollegeRelated;
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

        [HttpGet("cursos")]
        public async Task<ActionResult<dynamic>> GetCursos() => await _context.Cursos.ToListAsync();

        [HttpGet("curso/{id}")]
        public async Task<ActionResult<dynamic>> GetCurso([FromRoute] int id)
        {
            if (id == 0)
            {
                return BadRequest(new
                {
                    Message = "O id não pode ser 0."
                });
            }
            var c = await _context.Cursos.FirstOrDefaultAsync(c => c.IdCourse == id);
            if (c == null)
            {
                return NotFound(new
                {
                    Message = "O curso não foi encontrado."
                });
            }
            return Ok(c);
        }

        /*
        [NonAction]
        public async Task<List<Category>> GetQuantity(List<Category> categories)
        {
            var categoryIds = categories.Select(c => c.Id).ToList();
            var trackedCategories = await _context.Categories
                .Where(c => categoryIds.Contains(c.Id))
                .ToListAsync();

            foreach (var category in trackedCategories)
            {
                category.Quantity = await _context.PostHasCategories.CountAsync(phc => phc.CategoryID == category.Id);
            }

            await _context.SaveChangesAsync();
            return trackedCategories; 
        }
        */
    }
}
