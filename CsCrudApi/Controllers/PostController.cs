using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.PostRelated.Requests;
using CsCrudApi.Models.UserRelated;
using CsCrudApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

        [HttpPost("criar-post")]
        public async Task<ActionResult<dynamic>> CreatePost([FromBody] PostRequest request, [FromHeader] string token)
        {
            var post = request.Post;
            var categories = request.Categories;

            if (token == null)
            {
                return BadRequest("Token vazio.");
            }

            if (post == null)
            {
                return BadRequest("Solicitação sem post.");
            }

            if (post.Type != ETypePost.flash && string.IsNullOrEmpty(post.DcTitulo))
            {
                return BadRequest(new
                {
                    message = "Qualquer post que não seja do tipo rápido precisa de título."
                });
            }

            post.QuantityLikes = 0;
            post.PostDate = DateTime.Now;
            post.Guid = TokenServices.GenerateGUIDString();

            try
            {
                //Verificações de usuário
                User? user = await TokenServices.GetTokenUserAsync(claimsPrincipal: TokenServices.ValidateJwtToken(token), _context);
                if (user == null)
                {
                    return BadRequest(new
                    {
                        message = "Erro na identificação do usuário"
                    });
                }

                post.UserId = user.UserId;

                _context.Posts.Add(post);
                if (categories != null)
                {
                    foreach (int categoryId in categories)
                    {
                        // Verificar se a categoria existe no banco
                        var category = await _context.Categories.FindAsync(categoryId);

                        // Se não existir, criar uma nova categoria
                        if (category == null)
                        {
                            category = new Category
                            {
                                Id = categoryId,
                                Name = $"Categoria {categoryId}", // Define um nome padrão ou obtenha-o de outro lugar
                                Description = $"Descrição padrão para categoria {categoryId}", // Altere se necessário
                                Quantity = 0 // Inicialize os campos com valores padrão
                            };

                            _context.Categories.Add(category); // Adiciona ao contexto
                        }

                        // Criar a associação entre Post e Categoria
                        _context.PostHasCategories.Add(new PostHasCategory
                        {
                            PostGUID = post.Guid,
                            CategoryID = categoryId
                        });
                    }
                }

                // Salvar as alterações no banco
                await _context.SaveChangesAsync();

                return Ok(post); // Retorna o post criado
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2601)
                {
                    return await RetryCreatePost(post, token);
                }
                else
                {
                    Console.WriteLine("Erro não relacionado a SQL/Unicidade: " + ex.Message);
                    // Registra o erro e retorna uma mensagem amigável
                    return StatusCode(500, "Se chegou aqui, entrego nas mãos do senhor. Detalhes: " + ex.Message);
                }
            }
        }

        private async Task<ActionResult<dynamic>> RetryCreatePost(Post post, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new
                {
                    message = "Token vazio."
                });
            }

            //Verificações de usuário
            var user = await TokenServices.GetTokenUserAsync(claimsPrincipal: TokenServices.ValidateJwtToken(token), _context);
            if (user == null)
            {
                return BadRequest(new
                {
                    message = "Erro na identificação do usuário"
                });
            }

            try
            {
                post.Guid = TokenServices.GenerateGUIDString();
                post.UserId = user.UserId;
                // Adiciona o novo post
                _context.Posts.Add(post);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Post criado com sucesso após colisão de GUID", post });
            }
            catch (Exception ex)
            {
                // Lança exceções se a segunda tentativa também falhar
                return StatusCode(500, new { error = "Erro ao criar o post na segunda tentativa", details = ex.Message });
            }
        }

        [HttpGet("feed")]
        public async Task<ActionResult<dynamic>> Feed([FromHeader] string token, [FromQuery] int pageNumber, int pageSize)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new
                {
                    message = "O Token não pode estar vazio."
                });
            }

            // Ajustar o tamanho da página
            pageSize = pageSize > 100 ? 100 : (pageSize < 5 ? 5 : pageSize);
            pageNumber = pageNumber < 1 ? 1 : pageNumber;

            try
            {
                // Validar o token e obter o usuário associado
                var tokenData = TokenServices.ValidateJwtToken(token);
                var user = await TokenServices.GetTokenUserAsync(tokenData, _context);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "O usuário não foi encontrado."
                    });
                }

                var followingUserIds = await _context.UsersFollowing
                    .Where(f => f.CdFollower == user.UserId)
                    .Select(f => f.CdFollowed)
                    .ToListAsync();

                var usersRelated = await _context.Users
                    .Where(u => u.CdCidade == user.CdCidade || u.CdCampus == user.CdCampus || u.CursoId == user.CursoId)
                    .Select(u => u.UserId)
                    .ToListAsync();

                var posts = await _context.Posts
                    .Where(p =>
                        followingUserIds.Contains(p.UserId) || // Usuários que o atual segue
                        usersRelated.Contains(p.UserId)) // Mesmo curso
                    .OrderByDescending(p => p.PostDate) // Ordenar por data de postagem (mais recente primeiro)
                    .Skip((pageNumber - 1) * pageSize) // Paginação
                    .Take(pageSize) // Limitar pelo tamanho da página
                    .ToListAsync();

                return Ok(posts);
            }
            catch (Exception ex)
            {
                // Registrar o erro e retornar mensagem amigável
                Console.Error.WriteLine($"Erro ao gerar feed: {ex}");
                return StatusCode(500, new { message = "Um erro inesperado aconteceu.", error = ex.Message });
            }
        }


        [HttpGet("{guid}")]
        public async Task<ActionResult<dynamic>> ShowPost([FromRoute] string guid)
        {
            if (guid == null)
            {
                return BadRequest(new { message = "Post não informado." });
            }
            if (guid.Length != 32)
            {
                return BadRequest(new { message = "Guid inválido." });
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Guid == guid);
            if (post == null)
            {
                return NotFound(new { message = "Post não encontrado" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == post.UserId);
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado." });
            }

            var campus = await _context.Campi.FirstOrDefaultAsync(c => c.Id == user.CdCampus);
            if (campus == null)
            {
                return NotFound(new
                {
                    Message = "Campus não encontrado."
                });
            }
            post.QuantityLikes = await CountLikesAsync(post.Guid);
            var userFiltered = new
            {
                userId = user.UserId,
                nmAutor = user.NmSocial,
                grauEscolaridade = user.GrauEscolaridade,
                nmInstituicao = $"{campus.SgCampus} - {campus.CampusName}",
                cursoId = user.CursoId,
                tipoInteresse = user.TipoInteresse
            };

            return Ok(new
            {
                // ftPerfil = user.ftPerfil
                User = userFiltered,
                Post = post,
                Categories = await GetCategories(post.Guid)
            });
        }

        [HttpDelete("delete/{guid}")]
        public async Task<ActionResult<dynamic>> DeletePost([FromRoute] string guid, [FromBody] string token)
        {
            if (guid == null || guid.Length != 32)
            {
                return BadRequest(new
                {
                    Message = "O guid não pode estar vazio."
                });
            }

            if (token == null)
            {
                return BadRequest(new
                {
                    Message = "O token não pode ser vazio."
                });
            }

            try
            {
                var user = await TokenServices.GetTokenUserAsync(TokenServices.ValidateJwtToken(token), _context);

                if (user == null)
                {
                    return Unauthorized(new
                    {
                        Message = "Token não válido ou expirado."
                    });
                }

                var post = await _context.Posts.FirstOrDefaultAsync(post => post.Guid == guid);
                if (post == null)
                {
                    return NotFound(new
                    {
                        Message = "O post não foi encontrado."
                    });
                }

                if (post.UserId != user.UserId)
                {
                    return StatusCode(403, new
                    {
                        Message = "Você não tem permissão para excluir esse post."
                    });
                }

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    Message = "O post foi devidamente excluído."
                });

            } catch (Exception ex) {
                return StatusCode(500, new
                {
                    Message = $"Erro inesperado. Confira detalhes: {ex.Message}."
                });
            }
        }

        [HttpGet("buscar")]
        public async Task<ActionResult<List<object>>> SearchPostByTitle([FromQuery] string titlePart, int pageNumber, int pageSize)
        {
            if (string.IsNullOrEmpty(titlePart))
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
                var posts = await _context.Posts
                    .Where(p => EF.Functions.Like(p.DcTitulo, $"%{titlePart}%"))
                    .OrderByDescending(p => p.PostDate)
                    .Select(p => new
                    {
                        p.Guid,
                        p.UserId,
                        p.AreaId,
                        p.Type,
                        p.QuantityLikes,
                    })
                    .Skip((pageNumber - 1)* pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (posts.Count == 0)
                {
                    return NotFound(new
                    {
                        Message = "Nenhum post encontrado."
                    });
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro: {ex.Message}");
            }
        }

        [HttpGet("buscar/{categoryId}")]
        public async Task<ActionResult<List<object>>> SearchPostByCategory([FromRoute] int categoryId, [FromQuery] int pageNumber, int pageSize)
        {
            if (categoryId == 0)
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
                var postGuids = await _context.PostHasCategories
                    .Where(a => a.CategoryID == categoryId)
                    .Select(a => a.PostGUID)
                    .Distinct()
                    .ToListAsync();

                if (postGuids.Count == 0)
                {
                    return NotFound(new
                    {
                        Message = "Nenhum post com a categoria encontrado."
                    });
                }

                var posts = await _context.Posts
                    .Where(p => postGuids.Contains(p.Guid))
                    .OrderByDescending(p => p.PostDate) // Ordenar por data decrescente
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Guid,
                        p.UserId,
                        p.AreaId,
                        p.Type,
                        p.QuantityLikes,
                        p.PostDate,
                        p.DcTitulo
                    })
                    .ToListAsync();

                if (posts.Count == 0)
                {
                    return NotFound(new
                    {
                        Message = "Nenhum post encontrado na página especificada."
                    });
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro: {ex.Message}");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<List<PostRequest>> GetUserPosts([FromRoute] int userId, [FromQuery] int pageNumber, int pageSize)
        {
            var posts = await PaginatePosts(_context.Posts.AsQueryable(),
                pageNumber,
                pageSize,
                p => p.Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PostDate));

            posts = await CountLikesAsync(posts);

            var listPosts = new List<PostRequest>();
            foreach (Post p in posts)
            {
                var request = new PostRequest
                {
                    Post = p,
                    Categories = await GetCategories(p.Guid)
                };
                listPosts.Add(request);
            }
            return listPosts;
        }

        [HttpPost("like/{postguid}")]
        public async Task<ActionResult<dynamic>> LikePost([FromRoute] string postguid, [FromHeader] string token)
        {
            if (postguid.IsNullOrEmpty())
            {
                return BadRequest(new
                {
                    message = "Post nulo ou inválido."
                });
            }

            if (token.IsNullOrEmpty())
            {
                return BadRequest(new
                {
                    message = "Token nulo ou vazio."
                });
            }

            //Verificações de usuário
            var user = await TokenServices.GetTokenUserAsync(claimsPrincipal: TokenServices.ValidateJwtToken(token), _context);
            if (user == null)
            {
                return BadRequest(new
                {
                    message = "Erro na identificação do usuário"
                });
            }

            var like = await _context.PostLikes
            .FirstOrDefaultAsync(pl => pl.PostGuid == postguid && pl.UserId == user.UserId);
            if (like != null)
            {
                like.UpdatedAt = DateTime.UtcNow;

                if (like.IsActive == false)
                {
                    like.IsActive = true;
                }
                else
                {
                    like.IsActive = false;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "O like foi atualizado"
                });
            }

            try
            {

                UserLikesPost newLike = new()
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PostGuid = postguid,
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
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Erro inesperado: {ex}" });
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
        public async Task<int> CountLikesAsync(string postGuid)
        {
            int count = await _context.Posts.Where(p => p.Guid == postGuid).CountAsync();
            return count;
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
