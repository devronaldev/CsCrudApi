using System.Security.Claims;
using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.PostRelated.Requests;
using CsCrudApi.Services;
using CsCrudApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public PostController(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// Cria um novo post no sistema.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite que um usuário autenticado crie diferentes tipos de posts.
        /// Um post do tipo 'flash' não requer um título, enquanto outros tipos sim.
        /// O post pode ser associado a categorias existentes ou novas categorias podem ser criadas e associadas.
        /// Requer autenticação (Bearer Token) e é altamente recomendado usar HTTPS.
        /// </remarks>
        /// <param name="request">Os dados do post a ser criado.</param>
        /// <response code="200">Retorna o PostResponseDTO com os detalhes do post criado e suas categorias.</response>
        /// <response code="400">Retorna um ErrorDTO para requisições inválidas, validações falhas ou problemas de identificação do usuário.</response>
        /// <response code="401">Indica que o cliente não está autenticado.</response>
        /// <response code="500">Retorna um ErrorDTO em caso de exceção interna do servidor.</response>
        [Authorize]
        // [RequireHttps] 
        [HttpPost("criar-post")]
        [ProducesResponseType(typeof(PostResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PostResponseDTO>> CreatePost([FromBody] PostRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "VAL400INVALIDREQUEST",
                    ErrorDescription = "Dados da requisição são inválidos.",
                    Message = "Verifique os dados enviados para o post.",
                });
            }

            if (request.Type != ETypePost.flash && string.IsNullOrEmpty(request.Title))
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "VAL400TITLEMISSING",
                    ErrorDescription = "O título é obrigatório para posts que não sejam do tipo 'flash'.",
                    Message = "Um título é necessário para este tipo de post."
                });
            }

            try
            {
                var claimsPrincipal = HttpContext.User;
                var user = await TokenServices.GetTokenUserAsync(claimsPrincipal, _context);
                if (user == null)
                {
                    return BadRequest(new ErrorDTO
                    {
                        ErrorCode = "AUTH400USERNOTFOUND",
                        ErrorDescription = "O usuário associado ao token não foi encontrado no sistema.",
                        Message = "Erro na identificação do usuário. Verifique suas credenciais."
                    });
                }

                var post = (Post)request;
                post.UserId = user.UserId;
                _context.Posts.Add(post);

                if (request.Categories.Any())
                {
                    // Busca por categorias existentes baseadas nas descrições fornecidas
                    var existingCategories = await _context.Categories
                        .Where(c => request.Categories.Contains(c.Description))
                        .ToListAsync();

                    // Identifica e cria novas categorias que não existem no banco
                    var newCategories = request.Categories
                        .Where(categoryDescription => !existingCategories.Any(c =>
                            c.Description.Equals(categoryDescription,
                                StringComparison.OrdinalIgnoreCase))) // Comparação case-insensitive
                        .Select(categoryDescription => new Category(categoryDescription))
                        .ToList();

                    if (newCategories.Any())
                    {
                        _context.Categories.AddRange(newCategories);
                    }

                    
                    if (newCategories.Any())
                    {
                        await _context.SaveChangesAsync();
                        existingCategories.AddRange(newCategories);
                    }
                    
                    // Cria associações entre o Post e as Categorias (existentes e recém-criadas)
                    foreach (var category in existingCategories)
                    {
                        _context.PostHasCategories.Add(new PostHasCategory
                        {
                            PostGUID = post.Guid,
                            CategoryID = category.Id
                        });
                    }
                }

                await _context.SaveChangesAsync();
                
                return Ok(new PostResponseDTO
                {
                    Post = post,
                    Categories = await GetCategories(post.Guid)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar post: {ex.Message} - {ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDTO
                {
                    ErrorCode = "GEN500CREATEPOST",
                    ErrorDescription = "Ocorreu um erro interno inesperado ao tentar criar o post.",
                    Message = "Não foi possível criar o post. Por favor, tente novamente mais tarde.",
                });
            }
        }

        /// <summary>
        /// Retorna um post específico pelo seu GUID.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite a recuperação detalhada de um post, incluindo informações do usuário que o criou e do campus associado.
        /// </remarks>
        /// <param name="guid">O GUID único do post a ser recuperado sem hífens.</param>
        /// <response code="200">Retorna o objeto PostSearchByGuidDTO contendo o post, o usuário e as informações do campus.</response>
        /// <response code="400">Retorna um ErrorDTO se o GUID fornecido for inválido (nulo, vazio ou com comprimento incorreto).</response>
        /// <response code="404">Retorna um ErrorDTO se o post, o usuário ou o campus associado não for encontrado.</response>
        /// <response code="500">Retorna um ErrorDTO em caso de exceção interna do servidor.</response>
        [HttpGet("{guid}")]
        [ProducesResponseType(typeof(PostSearchByGuidDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PostResponseDTO>> GetPost([FromRoute] string guid)
        {
            try
            {
                if (string.IsNullOrEmpty(guid) || guid.Length != 32)
                {
                    return BadRequest(new ErrorDTO
                    {
                        ErrorCode = "VAL400INVALIDGUID",
                        ErrorDescription =
                            "O GUID do post é inválido. Ele não pode ser vazio e deve ter o formato correto (32 caracteres hexadecimais).",
                        Message = "O identificador do post fornecido é inválido."
                    });
                }

                var post = await _context.Posts.FirstOrDefaultAsync(p => p.Guid == guid); // Use parsedGuid
                if (post == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "RES404POSTNOTFOUND",
                        ErrorDescription = $"Post com GUID '{guid}' não encontrado.",
                        Message = "O post solicitado não foi encontrado."
                    });
                }
                var postResponse = new PostResponseDTO(post);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == post.UserId);
                if (user == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "RES404USERNOTFOUND",
                        ErrorDescription = $"Usuário com ID '{post.UserId}' associado ao post não encontrado.",
                        Message = "O usuário criador do post não foi encontrado."
                    });
                }
                
                var campus = await _context.Campi.FirstOrDefaultAsync(c => c.Id == user.CdCampus);
                if (campus == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "RES404CAMPUSNOTFOUND",
                        ErrorDescription = $"Campus com ID '{user.CdCampus}' associado ao usuário não encontrado.",
                        Message = "O campus associado ao usuário não foi encontrado."
                    });
                }
                var campusResponse = new CampusResponseDTO(campus);

                return Ok(new PostSearchByGuidDTO(campusResponse, user, postResponse));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro ao buscar post por GUID '{guid}': {e.Message} - {e.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDTO
                {
                    ErrorCode = "GEN500GETPOST",
                    ErrorDescription = $"Ocorreu um erro interno inesperado ao tentar buscar o post com GUID '{guid}'.",
                    Message = "Um erro inesperado ocorreu ao tentar obter o post."
                });
            }
        }

        /// <summary>
        /// Exclui um post através do seu GUID.
        /// </summary>
        /// <param name="guid">O identificador único (GUID) do post a ser excluído.</param>
        /// <returns>Um ActionResult indicando o resultado da operação de exclusão.</returns>
        /// <response code="204">O post foi excluído com sucesso.</response>
        /// <response code="400">O GUID fornecido é inválido ou vazio.</response>
        /// <response code="401">O token de autenticação é inválido ou expirado.</response>
        /// <response code="403">O usuário autenticado não possui permissão para excluir este post.</response>
        /// <response code="404">O post com o GUID especificado não foi encontrado.</response>
        /// <response code="500">Ocorreu um erro inesperado no servidor.</response>
        [Authorize]
        // [RequireHttps]
        [HttpDelete("delete/{guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 401)]
        [ProducesResponseType(typeof(ErrorDTO), 403)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult> DeletePost([FromRoute] string guid)
        {
            if (string.IsNullOrEmpty(guid) || guid.Length != 32)
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "VAL400GUIDINVALID",
                    ErrorDescription = "O GUID fornecido na rota está vazio ou não possui 32 caracteres.",
                    Message = "O GUID do post é inválido."
                });
            }

            ClaimsPrincipal claimsPrincipal = HttpContext.User;

            try
            {
                var user = await TokenServices.GetTokenUserAsync(claimsPrincipal, _context);
                if (user == null)
                {
                    return Unauthorized(new ErrorDTO
                    {
                        ErrorCode = "AUTH401TOKENINVALID",
                        ErrorDescription = "O token de autenticação não é válido ou está expirado, ou o usuário não foi encontrado.",
                        Message = "Token de autenticação inválido ou expirado."
                    });
                }

                var post = await _context.Posts.FirstOrDefaultAsync(post => post.Guid == guid);
                if (post == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "DATA404POSTNOTFOUND",
                        ErrorDescription = $"Nenhum post foi encontrado com o GUID: {guid}.",
                        Message = "O post não foi encontrado."
                    });
                }

                if (post.UserId != user.UserId)
                {
                    return StatusCode(403, new ErrorDTO
                    {
                        ErrorCode = "AUTH403PERMISSIONDENIED",
                        ErrorDescription = $"O usuário {user.UserId} tentou excluir o post {post.Guid} que pertence ao usuário {post.UserId}.",
                        Message = "Você não tem permissão para excluir esse post."
                    });
                }

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
                return NoContent();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir post: {ex.Message} - {ex.StackTrace}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500DELETEPOST",
                    ErrorDescription ="Ocorreu um erro interno inesperado ao tentar excluir o post.",
                    Message = "Não foi possível excluir o post. Por favor, tente novamente mais tarde."
                });
            }
        }

        /// <summary>
        /// Busca posts por uma parte do título.
        /// </summary>
        /// <param name="titlePart">A parte do título a ser buscada.</param>
        /// <param name="pageNumber">O número da página para paginação (padrão: 1).</param>
        /// <param name="pageSize">O tamanho da página para paginação (padrão: 5, máximo: 20).</param>
        /// <returns>Uma lista de posts que contêm a parte do título especificada.</returns>
        /// <response code="200">Retorna a lista de posts encontrados.</response>
        /// <response code="400">O campo de busca está vazio ou inválido.</response>
        /// <response code="500">Ocorreu um erro interno inesperado no servidor.</response>
        [Authorize]
        //[RequireHttps]
        [HttpGet("buscar-por-titulo")]
        [ProducesResponseType(typeof(List<SearchedPostDTO>), 200)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<List<SearchedPostDTO>>> SearchPostByTitle([FromQuery] string titlePart,
            int pageNumber = 1, int pageSize = 5)
        {
            if (string.IsNullOrEmpty(titlePart))
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "VAL400TITLEPARTEMPTY",
                    ErrorDescription = "O parâmetro 'titlePart' da query está vazio ou nulo.",
                    Message = "O campo de busca não pode estar vazio."
                });
            }

            // Garante que o tamanho da página não exceda 20 e seja no mínimo 1.
            pageSize = pageSize > 20 ? 20 : (pageSize < 1 ? 10 : pageSize);
            // Garante que o número da página seja no mínimo 1.
            pageNumber = pageNumber < 1 ? 1 : pageNumber;

            try
            {
                var posts = await _context.Posts
                    .Where(p => EF.Functions.Like(p.DcTitulo, $"%{titlePart}%"))
                    .OrderByDescending(p => p.PostDate)
                    .Select(p => new SearchedPostDTO(p))
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(posts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado ao buscar posts por título: {ex.Message} - {ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDTO
                {
                    ErrorCode = "GEN500SEARCHPOSTTITLE",
                    ErrorDescription =
                        $"Ocorreu um erro interno inesperado ao tentar buscar posts por título.",
                    Message = "Não foi possível realizar a busca por título. Por favor, tente novamente mais tarde."
                });
            }
        }

        /// <summary>
        /// Busca posts associados a categorias cujos nomes contêm uma parte específica.
        /// </summary>
        /// <param name="partName">A parte do nome da categoria a ser buscada.</param>
        /// <param name="pageNumber">O número da página para paginação (padrão: 1).</param>
        /// <param name="pageSize">O tamanho da página para paginação (padrão: 2, máximo: 20).</param>
        /// <returns>Uma lista paginada de posts que pertencem a categorias encontradas.</returns>
        /// <response code="200">Retorna a lista de posts encontrados.</response>
        /// <response code="400">O campo de busca está vazio.</response>
        /// <response code="401">O usuário não está autenticado.</response>
        /// <response code="404">Nenhuma categoria ou post encontrado com o filtro especificado.</response>
        /// <response code="500">Ocorreu um erro interno inesperado no servidor.</response>
        [Authorize]
        // [RequireHttps]
        [HttpGet("buscar-por-categorias")]
        [ProducesResponseType(typeof(List<SearchedPostDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<SearchedPostDTO>>> GetPosts([FromQuery] string partName, int pageNumber = 1,
            int pageSize = 2)
        {
            if (string.IsNullOrEmpty(partName))
            {
                return BadRequest(new ErrorDTO 
                {
                    ErrorCode = "VAL400CATEGORYPARTEMPTY",
                    ErrorDescription = "O parâmetro 'partName' da query está vazio ou nulo.",
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
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "DATA404CATEGORYNOTFOUND",
                        ErrorDescription = $"Nenhuma categoria encontrada com a parte do nome: '{partName}'.",
                        Message = "Nenhuma categoria encontrada com o filtro especificado."
                    });
                }

                var posts = await _context.Posts
                    .Where(p => _context.PostHasCategories.Any(pc =>
                        pc.PostGUID == p.Guid && categoriesId.Contains(pc.CategoryID)))
                    .OrderByDescending(p => p.PostDate)
                    .Select(p => new SearchedPostDTO(p))
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (!posts.Any())
                {
                    return NoContent();
                }
                
                return Ok(posts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado ao buscar posts por categorias: {ex.Message} - {ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorDTO 
                    {
                        ErrorCode = "GEN500SEARCHPOSTBYCATEGORY",
                        ErrorDescription =
                            $"Ocorreu um erro interno inesperado ao tentar buscar posts por categoria.",
                        Message =
                            "Não foi possível realizar a busca por categoria. Por favor, tente novamente mais tarde."
                    });
            }
        }

        /// <summary>
        /// Busca posts associados a uma categoria específica pelo seu ID.
        /// </summary>
        /// <param name="categoryId">O ID da categoria para a qual os posts serão buscados.</param>
        /// <param name="pageNumber">O número da página para paginação (padrão: 1).</param>
        /// <param name="pageSize">O tamanho da página para paginação (padrão: 5, máximo: 20).</param>
        /// <returns>Uma lista paginada de posts que pertencem à categoria especificada.</returns>
        /// <response code="200">Retorna a lista de posts encontrados.</response>
        /// <response code="400">O ID da categoria fornecido é inválido (igual a 0).</response>
        /// <response code="500">Ocorreu um erro interno inesperado no servidor.</response>
        [HttpGet("buscar/{categoryId}")]
        [ProducesResponseType(typeof(List<SearchedPostDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<SearchedPostDTO>>> SearchPostByCategory([FromRoute] int categoryId,
            [FromQuery] int pageNumber = 1, int pageSize = 5)
        {
            if (categoryId ==
                0) // Validando se o categoryId é 0, que geralmente é um valor inválido para IDs de banco de dados.
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "VAL400CATEGORYIDINVALID",
                    ErrorDescription = "O 'categoryId' fornecido na rota é inválido (igual a zero).",
                    Message = "O ID da categoria não pode ser zero."
                });
            }

            pageSize = pageSize > 20 ? 20 : (pageSize < 1 ? 10 : pageSize);
            pageNumber = pageNumber < 1 ? 1 : pageNumber;

            try
            {
                var posts = await _context.Posts
                    .Where(p => _context.PostHasCategories.Any(pc =>
                        pc.PostGUID == p.Guid && pc.CategoryID == categoryId))
                    .OrderByDescending(p => p.PostDate)
                    .Select(p => new SearchedPostDTO(p))
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (!posts.Any())
                {
                    return NoContent();
                }
                
                return Ok(posts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado ao buscar posts por categoria ID: {ex.Message} - {ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDTO
                {
                    ErrorCode = "GEN500SEARCHPOSTBYCATEGORYID",
                    ErrorDescription =
                        $"Ocorreu um erro interno inesperado ao tentar buscar posts pela categoria ID {categoryId}.",
                    Message = "Não foi possível realizar a busca por categoria. Por favor, tente novamente mais tarde."
                });
            }
        }

        /// <summary>
        /// Adiciona um 'like' a um post, ou atualiza o estado de um 'like' existente (ativa/desativa).
        /// </summary>
        /// <param name="guid">O GUID (identificador único) do post a ser 'curtido' ou cujo 'like' será atualizado.</param>
        /// <returns>Um ActionResult indicando o resultado da operação de 'like'.</returns>
        /// <response code="200">O 'like' foi adicionado ou atualizado com sucesso.</response>
        /// <response code="400">O GUID do post é nulo ou inválido, ou houve um erro na identificação do usuário.</response>
        /// <response code="401">O usuário não está autenticado.</response>
        /// <response code="500">Ocorreu um erro interno inesperado no servidor.</response>
        [HttpPost("like/{guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<dynamic>> LikePost([FromRoute] string guid)
        {
            if (string.IsNullOrEmpty(guid) || guid.Length != 32)
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "VAL400POSTGUIDINVALID",
                    ErrorDescription = "O GUID do post fornecido na rota está nulo ou não possui 32 caracteres.",
                    Message = "Post nulo ou inválido."
                });
            }

            var claimsPrincipal = HttpContext.User;

            try
            {
                //Verificações de usuário
                var user = await TokenServices.GetTokenUserAsync(claimsPrincipal, _context);
                if (user == null)
                {
                    return Unauthorized(new ErrorDTO
                    {
                        ErrorCode = "AUTH401USERIDENTIFICATIONFAILED",
                        ErrorDescription =
                            "O token de autenticação não é válido ou o usuário associado não foi encontrado.",
                        Message = "Erro na identificação do usuário. Verifique suas credenciais."
                    });
                }

                var like = await _context.PostLikes
                    .FirstOrDefaultAsync(pl => pl.PostGuid == guid && pl.UserId == user.UserId);
                if (like != null)
                {
                    like.UpdatedAt = DateTime.UtcNow;
                    like.IsActive = !like.IsActive;

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        Message = "O status do 'like' foi atualizado com sucesso."
                    });
                }

                UserLikesPost newLike = new()
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PostGuid = guid,
                    UserId = user.UserId,
                    IsActive = true,
                };

                _context.PostLikes.Add(newLike);
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    Message = "Like adicionado com sucesso."
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro inesperado ao dar 'like' no post: {e.Message} - {e.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDTO
                {
                    ErrorCode = "GEN500LIKEPOST",
                    ErrorDescription =
                        $"Ocorreu um erro interno inesperado ao tentar dar 'like' no post.",
                    Message = "Não foi possível processar o 'like'. Por favor, tente novamente mais tarde."
                });
            }
        }

        [NonAction]
        public static async Task<List<Post>> PaginatePosts(IQueryable<Post> query, int pageNumber, int pageSize, Func<IQueryable<Post>, IQueryable<Post>>? filter = null)
        {
            var items = await query.Skip((pageNumber - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            return items;
        }

        [NonAction]
        private async Task<int> CountLikesAsync(string postGuid)
        {
            return await _context.PostLikes.Where(l => l.PostGuid == postGuid).CountAsync();
        }

        [NonAction]
        public async Task<List<Post>> CountLikesAsync(List<Post> posts)
        {
            foreach(var post in posts)
            {
                post.QuantityLikes = await CountLikesAsync(post.Guid);
            }

            return posts;
        }

        [NonAction]
        private async Task<List<int>?> GetCategories(string guid)
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
