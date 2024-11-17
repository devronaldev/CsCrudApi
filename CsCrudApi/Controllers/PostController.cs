using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.PostRelated.Requests;
using CsCrudApi.Models.UserRelated;
using CsCrudApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public PostController(ApplicationDbContext context) => _context = context;

        [HttpPost("criar-post")]
        public async Task<ActionResult<dynamic>> CreatePost([FromBody] Post post, [FromHeader] string token)
        {
            if (token == null)
            {
                return BadRequest("Token vazio.");
            }

            if (post == null)
            {
                return BadRequest("Solicitação sem post.");
            }

            if (post.Type == ETypePost.flash && string.IsNullOrEmpty(post.DcTitulo))
            {
                return BadRequest(new
                {
                    message = "Qualquer post que não seja do tipo rápido precisa de título."
                });
            }

            post.QuantityLikes = 0;
            post.PostDate = DateTime.UtcNow;
            post.Guid = TokenServices.GenerateGUIDString();

            try
            {
                // Validação do Token
                var claimsPrincipal = TokenServices.ValidateJwtToken(token);
                if (claimsPrincipal == null)
                {
                    return BadRequest("Erro: Token inválido ou não pode ser validado.");
                }

                var emailClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimValueTypes.Email)?.Value;
                if (string.IsNullOrEmpty(emailClaim))
                {
                    return BadRequest("Erro: Token inválido, claim de e-mail ausente.");
                }

                var creatorUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);
                if (creatorUser == null)
                {
                    return NotFound("Usuário publicador não encontrado");
                }

                post.UserId = creatorUser.UserId;

                _context.Posts.Add(post);
                await _context.SaveChangesAsync(); // Salva o post primeiro para garantir que tenha um GUID disponível

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
                var claimsPrincipal = TokenServices.ValidateJwtToken(token);
            if (claimsPrincipal == null) {
                return BadRequest(new
                {
                    message = "Token danificado."
                });
            }
            var emailUser = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimValueTypes.Email)?.Value;
            if (emailUser == null)
            {
                return BadRequest(new
                {
                    message = "token danificado."
                });
            }
            var userCreator = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailUser);
            if (userCreator == null)
            {
                return NotFound(new
                {
                    message = "Usuário não encontrado."
                });
            }
            try
            {
                post.Guid = TokenServices.GenerateGUIDString();
                post.UserId = userCreator.UserId;
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

        [HttpGet("mostrar-post")]
        public async Task<ActionResult<dynamic>> ShowPost([FromQuery] string guid)
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

            return Ok(new
            {
                // post = guid, type, textPost, dcTitulo, (flDownload, qtLikes, qtComentarios) = add ao post.
                post,
                // ftPerfil = user.ftPerfil
                nmAutor = user.NmSocial,
                grauEscolaridade = user.GrauEscolaridade,
                tipoInteresse = user.TipoInteresse,
                // dcCategorias = user.Categorias
            });
        }

        [HttpPost("mais-posts")]
        public async Task<ActionResult<dynamic>> PostList([FromBody] PostRequest request, [FromHeader] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new
                {
                    message = "Token vazio."
                });
            }


            var posts = await PaginatePosts(_context.Posts.AsQueryable(), request.PageNumber, request.PageSize);

            return posts;
        }

        public static async Task<List<Post>> PaginatePosts(IQueryable<Post> query, int pageNumber, int pageSize, Func<IQueryable<Post>, IQueryable<Post>>? filter = null)
        {
            var items = await query.Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return items;
        }

        [HttpGet("{userId}")]
        public async Task<List<Post>> GetUserPosts([FromRoute] int userId, [FromQuery] int pageNumber, int pageSize)
        {
            var posts = await PaginatePosts(_context.Posts.AsQueryable(), 
                pageNumber, 
                pageSize, 
                p => p.Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PostDate));
            return posts;
        }
    } 
}
