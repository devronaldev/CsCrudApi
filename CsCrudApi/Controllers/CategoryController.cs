using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.PostRelated.Requests;
using CsCrudApi.Models.UserRelated;
using CsCrudApi.Models.UserRelated.CollegeRelated;
using CsCrudApi.Services;
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

        [HttpGet("feed-users")]
        public async Task<ActionResult<dynamic>> GetRecommendedUsers([FromHeader] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new
                {
                    message = "O token não pode estar vazio."
                });
            }

            try
            {
                var user = await TokenServices.GetTokenUserAsync(TokenServices.ValidateJwtToken(token), _context);

                if (user == null)
                {
                    return NotFound(new { message = "Usuário não encontrado." });
                }

                var followedIds = await _context.UsersFollowing
                    .Where(uf => uf.Status == true && uf.CdFollower == user.UserId)
                    .Select(uf => uf.CdFollowed)
                    .ToListAsync();

                var recommendedUsers = await _context.Users
                    .Where(u =>
                        (u.CdCampus == user.CdCampus || u.CdCidade == user.CdCidade || u.CursoId == user.CursoId) &&
                        !followedIds.Contains(u.UserId) &&
                        u.UserId != user.UserId)
                    .OrderBy(u => u.TipoInteresse)
                    .Take(2) // Limitar diretamente no banco
                    .Select(u => new
                    {
                        Name = u.NmSocial,
                        Id = u.UserId,
                        ProfilePicture = u.ProfilePictureUrl
                    })
                    .ToListAsync();

                if (!recommendedUsers.Any())
                {
                    return NotFound(new { message = "Nenhum usuário recomendado encontrado." });
                }

                return Ok(recommendedUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erro inesperado: {ex.Message}" });
            }
        }

        [HttpGet("feed-categories")]
        public async Task<ActionResult<dynamic>> GetRecommendedCategories([FromHeader] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new
                {
                    message = "O token não pode ser vazio."
                });
            }

            try
            {
                // Validar o token e obter o usuário
                var user = await TokenServices.GetTokenUserAsync(TokenServices.ValidateJwtToken(token), _context);
                if (user == null)
                {
                    return NotFound(new { message = "Usuário não encontrado." });
                }

                // Obter os posts do usuário atual
                var usersPosts = await _context.Posts
                    .Where(p => p.UserId == user.UserId)
                    .OrderBy(p => p.QuantityLikes)
                    .ToListAsync();

                // Se o usuário não tiver posts, buscar posts de usuários recomendados
                if (!usersPosts.Any())
                {
                    var recommendedUsersResponse = await GetRecommendedUsers(token);
                    if (recommendedUsersResponse.Result is ObjectResult result && result.Value is IEnumerable<dynamic> recommendedUsers)
                    {
                        var userIds = recommendedUsers.Select(u => (int)u.Id).ToList();
                        usersPosts = await _context.Posts
                            .Where(p => userIds.Contains(p.UserId))
                            .OrderBy(p => p.QuantityLikes)
                            .ToListAsync();
                    }
                }

                // Se ainda assim não houver posts, retornar vazio
                if (!usersPosts.Any())
                {
                    return Ok(new { message = "Nenhuma categoria recomendada encontrada." });
                }

                // Obter as categorias associadas aos posts encontrados
                var categories = await _context.PostHasCategories
                    .Where(phc => usersPosts.Select(p => p.Guid).Contains(phc.PostGUID))
                    .Select(phc => phc.CategoryID)
                    .Distinct()
                    .Take(2)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erro inesperado: {ex.Message}" });
            }
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
