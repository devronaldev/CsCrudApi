using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.PostRelated.Request;
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
        private readonly IConfiguration _configuration;
        public PostController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("criar-post")]
        public async Task<ActionResult<dynamic>> CreatePost([FromBody] PostCreationRequest model, [FromHeader] string token)
        {
            var post = model.Post;
            //var authorIds = model.PostAuthorsIds;

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

            /*if (authorIds == null || authorIds.Count <= 0)
            {
                return BadRequest("Sem autores.");
            }*/

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

                var emailClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                if (string.IsNullOrEmpty(emailClaim))
                {
                    return BadRequest("Erro: Token inválido, claim de e-mail ausente.");
                }

                var creatorUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);
                if (creatorUser == null)
                {
                    return NotFound("Usuário publicador não encontrado");
                }

                /*if (!authorIds.Contains(creatorUser.IdUser))
                {
                    return NotFound("Usuário publicador não pertence à lista de autores");
                }*/

                // Adiciona o post
                _context.Posts.Add(post);
                await _context.SaveChangesAsync(); // Salva o post primeiro para garantir que tenha um ID

                // Verifica se os autores já existem e adiciona os novos
                /*foreach (var authorId in authorIds.Distinct()) // Use Distinct para evitar duplicatas
                {
                    var authorExists = await _context.Users.AnyAsync(u => u.IdUser == authorId);
                    if (!authorExists)
                    {
                        return BadRequest($"Autor com ID {authorId} não encontrado.");
                    }

                    // Verifica se a combinação post e autor já existe
                    if (!await _context.PostAuthors.AnyAsync(a => a.CdUser == authorId && a.GuidPost == post.Guid))
                    {
                        _context.PostAuthors.Add(new PostAuthors { CdUser = authorId, GuidPost = post.Guid });
                    }
                }*/

                //CASO SEJA APENAS UM AUTOR, ALTERAR TABELA POST E ADICIONAR CHAVE ESTRANGEIRA LÁ
                _context.PostAuthors.Add(new PostAuthors { CdUser = creatorUser.IdUser, GuidPost = post.Guid });

                // Salva as mudanças na tabela PostAuthors
                await _context.SaveChangesAsync();

                return Ok(post); // Retorna o post criado
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2601) // Código de erro de violação de unicidade
                {
                    return await RetryCreatePost(post, token);
                }
                else
                {
                    // Registra o erro e retorna uma mensagem amigável
                    return StatusCode(500, "Se chegou aqui, entrego nas mãos do senhor. Detalhes: " + ex.Message);
                }
            }
        }



        private async Task<ActionResult<dynamic>> RetryCreatePost(Post post, string token)
        {
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
            var postCreator = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailUser);
            if (postCreator == null)
            {
                return NotFound(new
                {
                    message = "Usuário não encontrado."
                });
            }
            try
            {
                // Gera um novo GUID para o post
                post.Guid = TokenServices.GenerateGUIDString();

                // Adiciona o novo post
                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                // Associa os autores ao novo post
                /*foreach (var authorId in authorIds)
                {
                    var postAuthor = new PostAuthors
                    {
                        GuidPost = post.Guid,
                        CdUser = authorId
                    };

                    _context.PostAuthors.Add(postAuthor);
                }*/
                _context.PostAuthors.Add(new PostAuthors { GuidPost = post.Guid, CdUser = postCreator.IdUser });

                // Salva os autores
                await _context.SaveChangesAsync();

                return Ok(new { message = "Post criado com sucesso após colisão de GUID", post = post });
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
                return BadRequest(new { message = "Guid inválido."});
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Guid == guid);
            if (post == null)
            {
                return NotFound(new { message = "Post não encontrado" });
            }

            var authorship = await _context.PostAuthors.FirstOrDefaultAsync(a => a.GuidPost == guid);
            if (authorship == null)
            {
                return NotFound(new { message = "Autor não encontrado." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == authorship.CdUser);
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado." });
            }

            return Ok( new
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

        [HttpPost("post={guid}")]
        public async Task<ActionResult<dynamic>> GetPostDetails([FromRoute] string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return BadRequest(new
                {
                    message = "Guid nulo ou vazio."
                });
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Guid == guid);
            if (post == null)
            {
                return NotFound(new
                {
                    message = "Post não encontrado."
                });
            }

            var authorShip = await _context.PostAuthors.FirstOrDefaultAsync(a => a.GuidPost == guid);
            if (authorShip == null)
            {
                return NotFound(new
                {
                    message = "Autor não encontrado."
                });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == authorShip.CdUser);
            if (user == null)
            {
                return NotFound(new
                {
                    message = "Autor não encontrado."
                });
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


        [HttpPost("Mais-posts")]
        public async Task<ActionResult<dynamic>> PostList([FromBody] PostRequest request)
        {
            List<Post> posts = new();

            List<string> listaPosts = request.PostGUIDs;

            if (request.IdUser == null || request.IdUser == 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    var post = await _context.Posts
                        .Where(p => !listaPosts.Contains(p.Guid))
                        .OrderByDescending(p => p.PostDate)
                        .FirstOrDefaultAsync();

                    if (post == null)
                    {
                        break;
                    }

                    posts.Add(post);
                }

                foreach (var post in posts)
                {
                    listaPosts.Add(post.Guid);
                }
                return new
                {
                    posts,
                    listaPosts
                };
            }

            for (int i = 0; i < 10; i++)
            {
                var post = await _context.Posts.Where(p => !posts.Select(x => x.Guid).Contains(p.Guid))
                    .Where(p => !listaPosts.Contains(p.Guid))
                    .OrderByDescending(p => p.PostDate)
                    .FirstOrDefaultAsync();

                if (post == null)
                {
                    break;
                }
                posts.Add(post);
            }
            foreach (var post in posts)
            {
                listaPosts.Add(post.Guid);
            }

            return new
            {
                posts,
                listaPosts
            };
        }

        /*
         * public async Task<ActionResult<dynamic>> PostList(List<string> listaPosts)
        {
            List<Post> posts = new();

            for (int i = 0; i < 5; i++)
            {
                var post = await _context.Posts
                    .Where(p => !listaPosts.Contains(p.Guid));
                    //.OrderByDescending(p => p.PostDate)
                    //.FirstOrDefaultAsync();

                if (post == null)
                {
                    break;
                }

                posts.Add(post);
            }

            List<string> listaPosts = new List<string>();

            foreach (var post in posts)
            {
                listaPosts.Add(post.Guid);
            }
            return new { };
        } */
    } 
}
