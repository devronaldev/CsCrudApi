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
using CsCrudApi.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public CategoryController(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// Retorna uma lista de todas as categorias disponíveis.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite a recuperação de todas as categorias existentes no sistema.
        /// A lista pode estar vazia se não houver categorias cadastradas.
        /// </remarks>
        /// <response code="200">Retorna uma lista de objetos CategoryDTO.</response>
        /// <response code="204">Retorna No Content se não houver categorias (alternativa de resposta).</response>
        /// <response code="500">Retorna um ErrorDTO em caso de exceção interna do servidor.</response>
        [AllowAnonymous]
        // [RequireHttps]
        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<CategoryDTO>>> GetCategories()
        {
            try
            {
                var categories = await _context.Categories.Select(c => new CategoryDTO(c)).ToListAsync();

                if (!categories.Any())
                {
                    return NoContent();
                }

                return Ok(categories);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro ao listar categorias: {e.Message}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500LISTCATEGORIES",
                    ErrorDescription = $"Exceção não tratada ocorreu ao tentar listar categorias.",
                    Message = "Um erro inesperado ocorreu ao tentar listar as categorias."
                });
            }
        }

        /// <summary>
        /// Retorna uma categoria específica pelo seu ID.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite a recuperação de detalhes de uma única categoria com base no seu identificador.
        /// </remarks>
        /// <param name="id">O ID numérico da categoria a ser recuperada.</param>
        /// <response code="200">Retorna o objeto CategoryDTO da categoria encontrada.</response>
        /// <response code="400">Retorna um ErrorDTO se o ID fornecido for inválido (e.g., negativo ou zero, dependendo da validação).</response>
        /// <response code="404">Retorna um ErrorDTO se nenhuma categoria com o ID especificado for encontrada.</response>
        /// <response code="500">Retorna um ErrorDTO em caso de exceção interna do servidor.</response>
        [Authorize]
        // [RequireHttps]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Category>> GetCategory([FromRoute] int id)
        {
            try
            {
                if (id < 0)
                {
                    return BadRequest(new ErrorDTO
                    {
                        ErrorCode = "VAL400INVALIDID",
                        ErrorDescription = "O ID da categoria deve ser um número maior que zero.",
                        Message = "O ID fornecido para a categoria é inválido."
                    });
                }

                var c = await _context.Categories.FirstOrDefaultAsync(category => category.Id == id);
                if (c == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "RES404CATEGORYNOTFOUND",
                        ErrorDescription = $"Nenhuma categoria com o ID {id} foi encontrada.",
                        Message = "A categoria solicitada não foi encontrada."
                    });
                }
                return Ok(new CategoryDTO(c));
            }
            catch (Exception e)
            {
                // Logar a exceção para depuração
                Console.WriteLine($"Erro ao obter categoria por ID: {e.Message}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500GETCATEGORY",
                    ErrorDescription = $"Exceção não tratada ocorreu ao buscar categoria com o ID: {id}.",
                    Message = "Um erro inesperado ocorreu ao tentar obter a categoria."
                });
            }
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
