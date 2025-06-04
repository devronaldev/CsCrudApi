using System.Security.Claims;
using CsCrudApi.DTOs;
using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.UserRelated;
using CsCrudApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public FeedController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Obtém um feed de posts personalizado para o usuário autenticado.
        /// </summary>
        /// <remarks>
        /// Este endpoint retorna posts de usuários que o usuário autenticado segue,
        /// e também posts de usuários que compartilham a mesma cidade, campus ou curso.
        /// Os resultados são paginados e ordenados pelos posts mais recentes.
        /// É necessário um token de autenticação JWT válido.
        /// </remarks>
        /// <param name="pageNumber">O número da página de resultados a ser retornada (base 1). Padrão é 1.</param>
        /// <param name="pageSize">O número de posts por página. O valor é ajustado entre 5 e 100. Padrão é 10.</param>
        /// <returns>Uma lista paginada de objetos <see cref="PostRequestDTO"/> que compõem o feed do usuário.</returns>
        /// <response code="200">Retorna a lista de posts do feed com sucesso.</response>
        /// <response code="400">Parâmetros de paginação inválidos.</response>
        /// <response code="401">O usuário não está autenticado ou o token é inválido/expirado.</response>
        /// <response code="404">O usuário autenticado não foi encontrado no sistema ou nenhum post foi encontrado para o feed com os critérios fornecidos.</response>
        /// <response code="500">Ocorreu um erro interno no servidor ao gerar o feed.</response>
        [HttpGet("posts")]
        [Authorize]
        [RequireHttps]
        [ProducesResponseType(typeof(IEnumerable<PostResponseDTO>), 200)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 401)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<IEnumerable<PostResponseDTO>>> Feed(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                pageSize = Math.Clamp(pageSize, 5, 100); // Garante que pageSize esteja entre 5 e 100
                pageNumber = Math.Max(1, pageNumber);   // Garante que pageNumber seja pelo menos 1

                ClaimsPrincipal claimsPrincipal = HttpContext.User;
                var userEmail =  TokenServices.GetTokenEmailAsync(claimsPrincipal);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "AUTH404USERNOTFOUND",
                        Message = "O usuário autenticado não foi encontrado no sistema.",
                        ErrorDescription = "As credenciais são válidas, mas o usuário associado não existe."
                    });
                }

                // Obter IDs dos usuários que o atual segue
                var followingUserIds = await _context.UsersFollowing
                    .Where(f => f.CdFollower == user.UserId)
                    .Select(f => f.CdFollowed)
                    .ToListAsync();

                // Obter IDs de usuários com as mesmas relações (cidade, campus, curso)
                // TODO: Adicionar área.
                var usersRelated = await _context.Users
                    .Where(u => u.CdCidade == user.CdCidade || u.CdCampus == user.CdCampus || u.CursoId == user.CursoId)
                    .Select(u => u.UserId)
                    .ToListAsync();

                // Buscar posts combinando os critérios
                var posts = await _context.Posts
                    .Where(p =>
                        followingUserIds.Contains(p.UserId) || // Posts de usuários seguidos
                        usersRelated.Contains(p.UserId))       // Posts de usuários relacionados
                    .OrderByDescending(p => p.PostDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PostResponseDTO(p))
                    .ToListAsync();

                if (!posts.Any())
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "RES404POSTSNOTFOUND",
                        Message = "Nenhum post encontrado para o feed.",
                        ErrorDescription = $"Não foram encontrados posts que correspondam aos seus interesses para o usuário com ID: {user.UserId} e critérios de paginação."
                    });
                }
                
                posts = await CountLikesAsync(posts);
                foreach (PostResponseDTO post in posts)
                {
                    post.Categories = await GetCategories(post.Post.Guid);
                }
                return Ok(posts);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao gerar feed para usuário: {HttpContext.User.Identity?.Name ?? "Desconhecido"} - {ex}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500FEEDGENERATION",
                    Message = "Ocorreu um erro interno ao tentar gerar o feed.",
                    ErrorDescription = "Por favor, tente novamente mais tarde. Se o problema persistir, entre em contato com o suporte."
                });
            }
        }
        
        /// <summary>
        /// Obtém uma lista das categorias mais recentes associadas aos posts de um usuário.
        /// </summary>
        /// <remarks>
        /// Este endpoint busca os posts de um usuário específico, identifica todas as categorias
        /// associadas a esses posts e retorna uma lista de categorias únicas, ordenadas pela
        /// data de publicação do post mais recente.
        /// </remarks>
        /// <param name="userId">O ID único do usuário para o qual se deseja obter as categorias recentes.</param>
        /// <returns>Uma lista de objetos <see cref="Category"/> representando as categorias encontradas.</returns>
        /// <response code="200">Retorna a lista de categorias recentes com sucesso.</response>
        /// <response code="204">Retorna o código No Content pois o usuário não possui posts com categorias.</response>
        /// <response code="400">O ID do usuário fornecido é inválido.</response>
        /// <response code="404">Nenhuma categoria encontrada associada aos posts do usuário ou o usuário não possui posts.</response>
        /// <response code="500">Ocorreu um erro interno no servidor ao processar a requisição.</response>
        [HttpGet("categorias-recentes/{userId}")]
        [RequireHttps]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<Category>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<List<Category>>> RecentCategories([FromRoute] int userId)
        {
            try
            {
                if (userId <= 0) // Validação para IDs inválidos
                {
                    return BadRequest(new ErrorDTO
                    {
                        ErrorCode = "VAL400INVALIDUSERID",
                        Message = "ID de usuário inválido.",
                        ErrorDescription = "O ID do usuário deve ser um número inteiro positivo."
                    });
                }

                var posts = await _context
                    .Posts
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.PostDate)
                    .ToListAsync();

                if (!posts.Any()) // Verifica se o usuário não tem posts
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "RES404POSTSNOTFOUNDFORUSER",
                        Message = "Nenhum post encontrado para este usuário.",
                        ErrorDescription = $"Nenhum post foi encontrado para o usuário com ID: {userId}. Portanto, nenhuma categoria recente pode ser determinada."
                    });
                }

                List<int> categoriesIds = new List<int>(); // Inicialização explícita
                foreach (var post in posts)
                {
                    var postCategories = await GetCategories(post.Guid);
                    if (postCategories != null)
                    {
                        categoriesIds.AddRange(postCategories);
                    }
                }
                categoriesIds = categoriesIds.Distinct().ToList();
                
                if (!categoriesIds.Any())
                {
                    return NoContent();
                }
                var categories = await _context
                    .Categories
                    .Where(c => categoriesIds.Contains(c.Id)) // Supondo que Category.Id seja o ID correto
                    .ToListAsync();

                if (!categories.Any())
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "RES404CATEGORIESNOTMATCHED",
                        Message = "As categorias associadas não foram encontradas no banco de dados.", 
                        ErrorDescription = "IDs de categorias foram encontrados, mas não correspondem a categorias registradas."
                    });
                }

                return Ok(categories);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao obter categorias recentes para o usuário {userId}: {ex}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500RECENTCATEGORIES",
                    Message = "Ocorreu um erro interno ao tentar listar as categorias recentes.",
                    ErrorDescription = "Por favor, tente novamente mais tarde. Se o problema persistir, entre em contato com o suporte."
                });
            }
        }
        
        /// <summary>
        /// Obtém categorias de posts recomendadas para o usuário autenticado.
        /// </summary>
        /// <remarks>
        /// Este endpoint analisa os posts do usuário autenticado para identificar categorias
        /// preferenciais. Se o usuário não tiver posts, ele busca posts de usuários recomendados
        /// (por exemplo, com base em conexões sociais ou outros critérios) para derivar recomendações.
        /// As categorias são retornadas de forma distinta e limitada (atualmente, as 2 primeiras mais relevantes/recentes).
        /// Requer autenticação e conexão HTTPS.
        /// </remarks>
        /// <param name="token">O token JWT de autenticação Bearer enviado no cabeçalho da requisição.</param>
        /// <returns>Uma lista de objetos <see cref="Category"/> representando as categorias recomendadas.</returns>
        /// <response code="200">Retorna a lista de categorias recomendadas com sucesso. A lista pode estar vazia se nenhuma categoria for encontrada.</response>
        /// <response code="400">O token fornecido é inválido ou malformado.</response>
        /// <response code="401">O token de autenticação é inválido ou expirado.</response>
        /// <response code="404">O usuário autenticado não foi encontrado no sistema, ou nenhum post/categoria pôde ser recomendado.</response>
        /// <response code="500">Ocorreu um erro interno no servidor.</response>
        [HttpGet("categories")]
        [RequireHttps]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<Category>), 200)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 401)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<IEnumerable<Category>>> GetRecommendedCategories()
        {
            ClaimsPrincipal claimsPrincipal = HttpContext.User;
            try
            {
                var userEmail =  TokenServices.GetTokenEmailAsync(claimsPrincipal);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "AUTH404USERNOTFOUND",
                        Message = "Usuário autenticado não encontrado.",
                        ErrorDescription = "As credenciais do token são válidas, mas o usuário associado não existe no sistema."
                    });
                }

                List<int> sourceUserIds = new List<int>(); // IDs dos usuários cujos posts serão considerados
                sourceUserIds.Add(user.UserId); // Começa com o próprio usuário

                // Obter os posts do usuário atual
                var usersPosts = await _context.Posts
                    .Where(p => p.UserId == user.UserId)
                    .OrderByDescending(p => p.QuantityLikes) // Ordenar por likes para relevância
                    .ToListAsync();

                // Se o usuário não tiver posts, buscar posts de usuários recomendados
                if (!usersPosts.Any())
                {
                    var recommendedUsersResult = await GetRecommendedUsers(user.UserId);
                    if (recommendedUsersResult.Result is OkObjectResult okResult && okResult.Value is IEnumerable<SearchedUserInfo> recommendedUsers)
                    {
                        var recommendedUserIds = recommendedUsers.Select(u => u.UserId).ToList();
                        sourceUserIds.AddRange(recommendedUserIds); // Adiciona IDs de usuários recomendados

                        // Buscar posts dos usuários recomendados
                        usersPosts = await _context.Posts
                            .Where(p => recommendedUserIds.Contains(p.UserId))
                            .OrderByDescending(p => p.QuantityLikes)
                            .ToListAsync();
                    }
                    // Caso o GetRecommendedUsers retorne outro status ou não tenha usuários
                    else
                    {
                        // Aqui você pode decidir se retorna 404 imediatamente ou continua
                        // para retornar uma lista vazia de categorias.
                        // Para este caso, vamos continuar para tentar encontrar categorias
                        // a partir de uma lista vazia de posts, o que resultará em NotFound ou Ok(vazio) mais abaixo.
                    }
                }

                // Se ainda assim não houver posts (nem do usuário, nem dos recomendados), retornar 404
                if (!usersPosts.Any())
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "REC404NOCATEGORIES",
                        Message = "Nenhuma categoria recomendada encontrada.",
                        ErrorDescription = "Não foi possível encontrar posts relevantes para recomendar categorias."
                    });
                }

                // Obter as categorias associadas aos posts encontrados
                // Supondo que PostHasCategories tem CategoryID e PostGUID
                var categoriesIds = await _context.PostHasCategories
                    .Where(phc => usersPosts.Select(p => p.Guid).Contains(phc.PostGUID))
                    .Select(phc => phc.CategoryID)
                    .Distinct()
                    .OrderByDescending(id => id) // Ordenar de alguma forma para pegar os "2" mais relevantes
                    .Take(2) // Pegar apenas as 2 primeiras categorias mais relevantes/recentes
                    .ToListAsync();

                // Se nenhuma categoria foi encontrada após a filtragem
                if (!categoriesIds.Any())
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "REC404NOCATEGORIES",
                        Message = "Nenhuma categoria recomendada encontrada.",
                        ErrorDescription = "Posts foram encontrados, mas nenhuma categoria válida foi associada a eles."
                    });
                }

                var categories = await _context
                    .Categories
                    .Where(c => categoriesIds.Contains(c.Id)) // Supondo que Category.Id é o campo correto
                    .ToListAsync();

                // Caso as categorias encontradas pelos IDs não existam no DB (cenário raro)
                if (!categories.Any())
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "REC404CATEGORIESNOTMATCHED",
                        Message = "As categorias recomendadas não puderam ser recuperadas.",
                        ErrorDescription = "Os IDs das categorias foram identificados, mas as categorias correspondentes não foram encontradas no banco de dados."
                    });
                }

                return Ok(categories); // Retorna 200 OK com a lista de categorias
            }
            catch (Exception ex)
            {
                // Registrar o erro completo para depuração (no log da aplicação)
                Console.Error.WriteLine($"Erro ao obter categorias recomendadas: {ex}");

                // Retornar um ErrorDTO padronizado sem detalhes internos da exceção
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500RECOMMENDEDCATEGORIES",
                    Message = "Ocorreu um erro interno ao tentar obter as categorias recomendadas.",
                    ErrorDescription = "Por favor, tente novamente mais tarde. Se o problema persistir, entre em contato com o suporte."
                });
            }
        }
        
        /// <summary>
        /// Obtém uma lista de usuários recomendados com base nas conexões e interesses do usuário especificado ou autenticado.
        /// </summary>
        /// <remarks>
        /// Este endpoint pode operar de duas formas:
        /// 1. Se um `userId` válido for fornecido, ele busca recomendações para aquele usuário.
        /// 2. Se nenhum `userId` for fornecido (ou for -1), ele usa o ID do usuário autenticado para gerar recomendações.
        /// As recomendações são baseadas em usuários que compartilham o mesmo campus, cidade ou curso,
        /// excluindo aqueles que já são seguidos ou o próprio usuário. Um máximo de 2 usuários são retornados.
        /// Requer autenticação.
        /// </remarks>
        /// <param name="userId">
        /// O ID único do usuário para o qual as recomendações serão geradas.
        /// Se omitido ou -1, o ID do usuário autenticado será utilizado.
        /// </param>
        /// <returns>Uma lista de objetos <see cref="SearchedUserInfo"/> representando os usuários recomendados.</returns>
        /// <response code="200">Retorna a lista de usuários recomendados com sucesso. A lista pode estar vazia.</response>
        /// <response code="204">Nenhum usuário recomendado encontrado com os critérios especificados.</response>
        /// <response code="400">O ID do usuário fornecido via query é inválido.</response>
        /// <response code="401">O usuário não está autenticado ou o token é inválido/expirado.</response>
        /// <response code="404">O usuário (especificado ou autenticado) não foi encontrado no sistema, ou nenhum usuário recomendado foi encontrado com os critérios.</response>
        /// <response code="500">Ocorreu um erro interno no servidor ao gerar as recomendações de usuários.</response>
        [HttpGet("users")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<SearchedUserInfo>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 401)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<IEnumerable<SearchedUserInfo>>> GetRecommendedUsers([FromQuery] int userId = -1)
        {
            User targetUser = null;
            try
            {
                if (userId == -1)
                {
                    ClaimsPrincipal claimsPrincipal = HttpContext.User;
                    var userEmail =  TokenServices.GetTokenEmailAsync(claimsPrincipal);
                    targetUser = _context.Users.FirstOrDefault(u => u.Email == userEmail);

                    if (targetUser == null)
                    {
                        return NotFound(new ErrorDTO
                        {
                            ErrorCode = "AUTH404USERNOTFOUND",
                            Message = "Usuário autenticado não encontrado.",
                            ErrorDescription = "As credenciais do token são válidas, mas o usuário associado não existe no sistema."
                        });
                    }
                }
                if (userId >= 0)
                {
                    targetUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (targetUser == null)
                    {
                        return NotFound(new ErrorDTO
                        {
                            ErrorCode = "USER404TARGETNOTFOUND",
                            Message = "O usuário alvo para recomendação não foi encontrado.",
                            ErrorDescription = $"Nenhum usuário encontrado com o ID: {userId} para gerar recomendações."
                        });
                    }
                }
                else 
                {
                    return BadRequest(new ErrorDTO
                    {
                        ErrorCode = "VAL400INVALIDUSERID",
                        Message = "ID de usuário inválido na requisição.",
                        ErrorDescription = "O ID do usuário fornecido na query deve ser um número inteiro positivo ou omitido."
                    });
                }
                
                var followedIds = await _context.UsersFollowing
                    .Where(uf => uf.Status == true && uf.CdFollower == targetUser.UserId)
                    .Select(uf => uf.CdFollowed)
                    .ToListAsync();

                var recommendedUsers = await _context.Users
                    .Where(u =>
                        // Usuários com alguma relação
                        (u.CdCampus == targetUser.CdCampus || u.CdCidade == targetUser.CdCidade || u.CursoId == targetUser.CursoId) &&
                        // Excluir usuários já seguidos
                        !followedIds.Contains(u.UserId) &&
                        // Excluir o próprio usuário
                        u.UserId != targetUser.UserId)
                    .OrderBy(u => u.TipoInteresse) // Sua lógica de ordenação
                    .Take(2) // Limitar a 2 usuários
                    .Select(u => new SearchedUserInfo(u))
                    .ToListAsync();

                if (!recommendedUsers.Any())
                {
                    return NoContent();
                }

                return Ok(recommendedUsers);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao obter usuários recomendados: {ex}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500GETRECOMMENDEDUSERS",
                    Message = "Ocorreu um erro interno ao tentar obter usuários recomendados.",
                    ErrorDescription = "Por favor, tente novamente mais tarde. Se o problema persistir, entre em contato com o suporte."
                });
            }
        }
        
        [NonAction]
        public async Task<int> CountLikesAsync(string postGuid)
        {
            int count = await _context.PostLikes.Where(l => l.PostGuid == postGuid).CountAsync();
            return count;
        }
        
        [NonAction]
        public async Task<List<PostResponseDTO>> CountLikesAsync(List<PostResponseDTO> posts)
        {
            foreach(var post in posts)
            {
                post.Post.QuantityLikes = await CountLikesAsync(post.Post.Guid);
            }

            return posts;
        }
        
        [NonAction]
        public async Task<List<int>?> GetCategories(string guid)
        {
            if (string.IsNullOrEmpty(guid))
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
        protected async Task<int> GetFollowers(int idUser) => await _context.UsersFollowing.CountAsync(u => u.CdFollowed == idUser);
        
        [NonAction]
        protected async Task<int> GetFollowing(int idUser) => await _context.UsersFollowing.CountAsync(u => u.CdFollower == idUser);
    }
}