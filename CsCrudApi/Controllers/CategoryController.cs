using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.PostRelated.Requests;
using CsCrudApi.Models.UserRelated.CollegeRelated;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Linq;

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

        [HttpGet("buscar")]
        public async Task<ActionResult<dynamic>> GetPosts([FromQuery] string partName, int pageNumber, int pageSize)
        {
            if (string.IsNullOrEmpty(partName))
            {
                return BadRequest(new
                {
                    Message = "O campo de busca não pode estar vazio."
                });
            }

            pageSize = pageSize > 20 ? 20 : (pageSize < 1 ? 10 : pageSize);
            pageNumber = pageNumber < 1 ? 1 : pageNumber;

            try
            {
                var categoriesId = await _context.Categories
                    .Where(c => EF.Functions.Like(c.Name, $"%{partName}%"))
                    .Select(c => c.Id)
                    .ToListAsync(); 

                if (!categoriesId.Any())
                {
                    return NotFound(new { message = "Nenhuma categoria encontrada com o filtro especificado." });
                }

                var posts = await _context.Posts
                    .Where(p => _context.PostHasCategories
                    .Any(pc => pc.PostGUID == p.Guid && categoriesId.Contains(pc.CategoryID)))
                    .OrderByDescending(p => p.PostDate) // Ordenação por data de postagem (mais recente primeiro)
                    .Skip((pageNumber - 1) * pageSize) // Paginação
                    .Take(pageSize) // Limitação de tamanho da página
                    .ToListAsync();

                posts = await CountLikesAsync(posts);

                var postRequests = new List<PostRequest>();
                foreach (Post p in posts)
                {
                    var request = new PostRequest
                    {
                        Post = p,
                        Categories = await GetCategories(p.Guid)
                    };
                    postRequests.Add(request);
                }

                var listPosts = new List<FeedPost>();

                foreach (var post in postRequests)
                {
                    listPosts.Add(new FeedPost
                    {
                        Post = post.Post,
                        Categories = post.Categories,
                        User = await GetUser(post.Post.UserId)
                    });
                }

                return Ok(listPosts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro: {ex.Message}");
            }
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

        [NonAction]
        public async Task<int> CountLikesAsync(string postGuid)
        {
            int count = await _context.PostLikes.Where(l => l.PostGuid == postGuid).CountAsync();
            return count;
        }

        [NonAction]
        public async Task<List<Post>> CountLikesAsync(List<Post> posts)
        {
            foreach (var post in posts)
            {
                post.QuantityLikes = await CountLikesAsync(post.Guid);
            }

            return posts;
        }

        [NonAction]
        public async Task<object> GetUser(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            return new
            {
                user.NmSocial,
                user.TipoInteresse,
                user.CdCampus,
                user.UserId,
                user.GrauEscolaridade,
                user.ProfilePictureUrl,
            };
        }
    }
}
