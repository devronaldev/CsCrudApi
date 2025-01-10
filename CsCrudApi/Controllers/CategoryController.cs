using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.UserRelated.CollegeRelated;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

        [HttpGet("recentes/{idUser}")]
        public async Task<ActionResult<dynamic>> RecentCategories(int idUser)
        {
            if (idUser == 0)
            {
                BadRequest(new
                {
                    Message = "Usuário não especificado."
                });
            }

            List<int> categoriesIds = [];

            var posts = await _context
                .Posts
                .Where(p => p.UserId == idUser)
                .OrderByDescending(p => p.PostDate)
                .ToListAsync();

            foreach (var post in posts)
            {
                var postCategories = await GetCategories(post.Guid);
                if (postCategories != null)
                {
                    categoriesIds.AddRange(postCategories);
                }
            }

            categoriesIds = categoriesIds.Distinct().ToList();

            var categories = await _context
                .Categories
                .Where(c => categoriesIds.Contains(c.Id))
                .ToListAsync();

            return Ok(categories);
        }

        [NonAction]
        public async Task<List<int>?> GetCategories(string guid)
        {
            if (guid.IsNullOrEmpty())
            {
                return null;
            }

            var phc = await _context.PostHasCategories.Where(p => p.PostGUID == guid).ToListAsync();

            List<int> dcCategories = [];
            foreach (var category in phc)
            {
                dcCategories.Add(category.CategoryID);
            }
            return dcCategories;
        }
    }
}
