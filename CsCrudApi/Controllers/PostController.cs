using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
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
            var authorIds = model.PostAuthorsIds;

            if (token == null)
            {
                return BadRequest("Token vazio.");
            }

            if (post == null)
            {
                return BadRequest("Solicitação sem post.");
            }

            if (authorIds == null || authorIds.Count <= 0)
            {
                return BadRequest("Sem autores.");
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

                if (!authorIds.Contains(creatorUser.IdUser))
                {
                    return NotFound("Usuário publicador não pertence à lista de autores");
                }

                // Adiciona o post
                _context.Posts.Add(post);
                await _context.SaveChangesAsync(); // Salva o post primeiro para garantir que tenha um ID

                // Verifica se os autores já existem e adiciona os novos
                foreach (var authorId in authorIds.Distinct()) // Use Distinct para evitar duplicatas
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
                }

                // Salva as mudanças na tabela PostAuthors
                await _context.SaveChangesAsync();

                return Ok(post); // Retorna o post criado
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2601) // Código de erro de violação de unicidade
                {
                    return await RetryCreatePost(post, authorIds);
                }
                else
                {
                    // Registra o erro e retorna uma mensagem amigável
                    return StatusCode(500, "Se chegou aqui, entrego nas mãos do senhor. Detalhes: " + ex.Message);
                }
            }
        }



        private async Task<ActionResult<dynamic>> RetryCreatePost(Post post, List<int> authorIds)
        {
            try
            {
                // Gera um novo GUID para o post
                post.Guid = TokenServices.GenerateGUIDString();

                // Adiciona o novo post
                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                // Associa os autores ao novo post
                foreach (var authorId in authorIds)
                {
                    var postAuthor = new PostAuthors
                    {
                        GuidPost = post.Guid,
                        CdUser = authorId
                    };

                    _context.PostAuthors.Add(postAuthor);
                }

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

        public async Task<ActionResult<dynamic>> PostList(List<string> listaPosts)
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
        }
    }
}
