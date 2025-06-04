using System.Security.Claims;
using CsCrudApi.DTOs;
using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.PostRelated.Requests;
using CsCrudApi.Models.UserRelated;
using CsCrudApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Retorna os detalhes do perfil de um usuário específico.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite que qualquer pessoa visualize informações públicas de um perfil de usuário,
        /// incluindo nome social, data de nascimento, e-mail, URL da foto de perfil,
        /// preferências, curso, escolaridade, campus e a contagem de seguidores e de quem o usuário segue.
        /// Retorna um erro 404 se o usuário, a cidade ou o campus associado não forem encontrados no sistema.
        /// </remarks>
        /// <param name="userId">O ID único do usuário cujo perfil será consultado.</param>
        /// <returns>
        /// Retorna uma instância de <see cref="UserProfileDTO"/> se o perfil for encontrado.
        /// Retorna <see cref="StatusCodes.Status404NotFound"/> com um <see cref="ErrorDTO"/>
        /// se o usuário, a cidade ou o campus não existirem.
        /// </returns>
        /// <response code="200">Retorna com sucesso o <see cref="UserProfileDTO"/> com os dados do perfil.</response>
        /// <response code="404">Retorna <see cref="ErrorDTO"/> indicando que o usuário, cidade ou campus não foi encontrado.</response>
        [HttpGet("{userId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserProfileDTO), StatusCodes.Status200OK)] // Agora retorna UserProfileDTO
        [ProducesResponseType(typeof(ErrorDTO), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProfileDTO>> Profile([FromRoute] int userId) // Tipo de retorno explícito
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new ErrorDTO
                {
                    ErrorCode = "USER404",
                    Message = "Usuário não encontrado.",
                    ErrorDescription = $"Não foi encontrado usuário com o ID '{userId}'."
                });
            }

            var cidade = await _context.Cidades.FirstOrDefaultAsync(c => c.IdCidade == user.CdCidade);

            if (cidade == null)
            {
                return NotFound(new ErrorDTO
                {
                    ErrorCode = "CITY404",
                    Message = "Cidade do usuário não encontrada.",
                    ErrorDescription =
                        $"A cidade associada ao usuário (ID: {user.CdCidade}) não foi encontrada ou está incorreta."
                });
            }

            var campus = await _context.Campi.FirstOrDefaultAsync(campi => campi.Id == user.CdCampus);

            if (campus == null)
            {
                return NotFound(new ErrorDTO
                {
                    ErrorCode = "CAMPUS404",
                    Message = "Campus do usuário não encontrado.",
                    ErrorDescription =
                        $"O campus associado ao usuário (ID: {user.CdCampus}) não foi encontrado ou está incorreto."
                });
            }

            var followers = await GetFollowers(user.UserId);
            var following = await GetFollowing(user.UserId);

            // Usa o construtor do DTO para criar a instância
            var userProfile = new UserProfileDTO(user, cidade, campus, followers, following);

            return Ok(userProfile);
        }
        
        /// <summary>
        /// Busca usuários pelo nome social.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite buscar usuários pelo nome social (NmSocial).
        /// A busca retorna apenas usuários com o e-mail verificado (`IsEmailVerified = true`).
        /// A paginação é suportada através dos parâmetros `pageNumber` e `pageSize`.
        /// </remarks>
        /// <param name="namePart">Parte do nome social a ser buscada (case-insensitive).</param>
        /// <param name="pageNumber">Número da página a ser retornada (começa em 1).</param>
        /// <param name="pageSize">Número de usuários por página (limite máximo de 10).</param>
        /// <returns>
        /// Retorna um status 200 OK com uma lista de usuários encontrados.
        /// Retorna um status 400 Bad Request se o campo de busca estiver vazio ou os parâmetros de paginação forem inválidos.
        /// Retorna um status 404 Not Found se nenhum usuário for encontrado com o critério de busca.
        /// Retorna um status 500 Internal Server Error em caso de erro inesperado no servidor.
        /// </returns>
        /// <response code="200">Lista de usuários encontrados.</response>
        /// <response code="400">Parâmetros de requisição inválidos (campo de busca vazio ou paginação fora dos limites).</response>
        /// <response code="404">Nenhum usuário encontrado para o critério de busca.</response>
        /// <response code="500">Erro interno do servidor.</response>
        [HttpGet("buscar")]
        [ProducesResponseType(typeof(List<SearchedUserInfo>), 200)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<List<SearchedUserInfo>>> SearchUsersByName(
            [FromQuery] string namePart,
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize)
        {
            if (string.IsNullOrEmpty(namePart))
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400NULL",
                    Message = "O campo de busca não pode estar vazio.",
                    ErrorDescription = "O parâmetro 'namePart' da query string é obrigatório."
                });
            }

            // Garante que pageSize esteja entre 1 e 10
            pageSize = pageSize > 10 ? 10 : (pageSize < 1 ? 1 : pageSize);
            // Garante que pageNumber seja no mínimo 1
            pageNumber = pageNumber < 1 ? 1 : pageNumber;

            try
            {
                List<SearchedUserInfo> users = await _context.Users
                    .Where(u => EF.Functions.Like(u.NmSocial, $"%{namePart}%") && u.IsEmailVerified == true)
                    .Select(u => new SearchedUserInfo(u))
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (users.Count == 0)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "404USERS",
                        Message = "Nenhum usuário encontrado.",
                        ErrorDescription = $"Nenhum usuário com 'NmSocial' contendo '{namePart}' foi encontrado."
                    });
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "500GENERIC",
                    Message = "Ocorreu um erro interno no servidor. Tente novamente mais tarde.",
                    ErrorDescription = "Erro inesperado e não tratado."
                });
            }
        }
        
        /// <summary>
        /// Permite que um usuário autenticado siga ou deixe de seguir outro usuário.
        /// </summary>
        /// <remarks>
        /// Este endpoint gerencia a relação de "seguir" entre usuários.
        /// O usuário que realiza a ação é automaticamente identificado através do token de autenticação
        /// fornecido no cabeçalho da requisição, conforme definido pela política de autorização.
        /// Se o usuário autenticado já segue (ou parou de seguir) o usuário alvo,
        /// a relação será invertida (deixar de seguir ou voltar a seguir) e um status 204 No Content será retornado.
        /// Se a relação de "seguir" não existe, uma nova será criada e um status 201 Created será retornado.
        /// </remarks>
        /// <param name="userId">O ID do usuário que será seguido ou deixado de seguir.</param>
        /// <returns>
        /// Retorna um status 201 Created se uma nova relação de "seguir" for estabelecida.
        /// Retorna um status 204 No Content se uma relação existente for alternada (de seguir para não seguir, ou vice-versa).
        /// Retorna um status 401 Unauthorized se o token de autenticação for ausente ou inválido.
        /// Retorna um status 404 Not Found se o usuário autenticado ou o usuário alvo não forem encontrados.
        /// Retorna um status 409 Conflict se o usuário tentar seguir a si mesmo.
        /// Retorna um status 500 Internal Server Error em caso de erro inesperado no servidor.
        /// </returns>
        /// <response code="201">Nova relação de "seguir" criada com sucesso.</response>
        /// <response code="204">Status da relação de "seguir" alternado com sucesso.</response>
        /// <response code="401">Não autorizado (token JWT ausente ou inválido no cabeçalho `Authorization`).</response>
        /// <response code="404">Usuário autenticado não encontrado ou usuário alvo não existe.</response>
        /// <response code="409">Tentativa de seguir o próprio usuário.</response>
        /// <response code="500">Erro interno do servidor.</response>
        [Authorize]
        [RequireHttps]
        [HttpPost("seguir/{userId}")]
        [ProducesResponseType(201)] // Para Created()
        [ProducesResponseType(204)] // Para NoContent()
        [ProducesResponseType(typeof(ErrorDTO), 401)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 409)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult> Follow([FromRoute] int userId)
        {
            try
            {
                // Acessa o ClaimsPrincipal do usuário autenticado via [Authorize]
                ClaimsPrincipal claimsPrincipal = HttpContext.User;

                // A validação de IsAuthenticated é redundante com [Authorize], mas é um bom fallback/segurança.
                if (claimsPrincipal == null || !claimsPrincipal.Identity.IsAuthenticated)
                {
                    return Unauthorized(new ErrorDTO
                    {
                        ErrorCode = "AUTH401",
                        Message = "Não autorizado.",
                        ErrorDescription = "Token de autenticação ausente ou inválido."
                    });
                }

                var userEmail =  TokenServices.GetTokenEmailAsync(claimsPrincipal);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);;
                if (user == null)
                {
                    // Isso pode acontecer se o usuário foi excluído após a emissão do token.
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "404USERAUTH",
                        Message = "Usuário autenticado não encontrado.",
                        ErrorDescription = "O usuário associado ao token de autenticação não foi encontrado no sistema."
                    });
                }

                if (user.UserId == userId)
                {
                    return Conflict(new ErrorDTO
                    {
                        ErrorCode = "409SELF",
                        Message = "O usuário não pode seguir a si mesmo.",
                        ErrorDescription = "Tentativa de seguir o próprio usuário, o que não é permitido."
                    });
                }

                if (!await _context.Users.AnyAsync(u => u.UserId == userId))
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "404TARGETUSER",
                        Message = "Usuário a ser seguido não existe.",
                        ErrorDescription = "O ID do usuário alvo fornecido não corresponde a nenhum usuário existente."
                    });
                }

                // BUSCA POR QUALQUER RELAÇÃO EXISTENTE, INDEPENDENTEMENTE DO STATUS
                var follow = await _context.UsersFollowing
                    .FirstOrDefaultAsync(f =>
                        f.CdFollower == user.UserId &&
                        f.CdFollowed == userId);

                // Se uma relação EXISTE (Status=true ou Status=false)
                if (follow != null)
                {
                    // Alterna o status
                    follow.Status = !follow.Status;
                    follow.LastUpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Retorna 204 No Content pois o estado de um recurso existente foi alterado
                    return NoContent();
                }

                // Se a relação NÃO EXISTE, cria uma nova
                var action = new UserFollowingUser
                {
                    CdFollowed = userId,
                    CdFollower = user.UserId,
                    Status = true, // Uma nova relação é sempre de "seguindo" inicialmente
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };

                _context.UsersFollowing.Add(action);
                await _context.SaveChangesAsync();

                // Retorna 201 Created pois um novo recurso foi adicionado
                return Created();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "500GENERIC",
                    Message = "Ocorreu um erro interno no servidor. Tente novamente mais tarde.",
                    ErrorDescription = "Erro inesperado." 
                });
            }
        }
        
        
        /// <summary>
        /// Obtém uma lista paginada de posts de um usuário específico.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite buscar posts de um usuário por seu ID, aplicando paginação
        /// para controlar a quantidade de resultados retornados. Os posts são ordenados
        /// por data de publicação de forma decrescente.
        /// </remarks>
        /// <param name="userId">O ID único do usuário cujos posts serão buscados.</param>
        /// <param name="pageNumber">O número da página de resultados a ser retornada (base 1).</param>
        /// <param name="pageSize">O número de posts por página.</param>
        /// <returns>Uma lista paginada de objetos <see cref="PostRequestDTO"/>.</returns>
        /// <response code="200">Retorna a lista de posts do usuário com sucesso.</response>
        /// <response code="400">Parâmetros de requisição inválidos (ex: ID de usuário ou paginação inválidos).</response>
        /// <response code="404">Nenhum post encontrado para o usuário ou com os critérios de paginação.</response>
        /// <response code="500">Ocorreu um erro interno no servidor.</response>
        [HttpGet("posts/{userId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PostResponseDTO>), 200)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<IEnumerable<PostResponseDTO>>> GetUserPosts(
            [FromRoute] int userId,
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10)  
        {
            if (userId <= 0)
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "VAL400INVALIDUSERID",
                    Message = "ID de usuário inválido.",
                    ErrorDescription = "O ID do usuário deve ser um número inteiro positivo."
                });
            }
            try
            {
                // Validação de paginação
                if (pageNumber <= 0) pageNumber = 1; // Garante que a página mínima é 1
                if (pageSize <= 0 || pageSize > 100) pageSize = 10; // Limita o tamanho da página para evitar abuso

                var posts = await _context.Posts
                    .Where(p => p.UserId == userId)
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
                        Message = "Nenhum post encontrado para este usuário ou nos critérios de paginação.",
                        ErrorDescription = $"Não foram encontrados posts para o usuário com ID: {userId} ou na página/tamanho especificado."
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
                Console.WriteLine($"Erro ao obter posts do usuário: {ex.Message}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500LISTUSERPOSTS",
                    Message = "Não foi possível listar os posts do usuário.",
                    ErrorDescription = "Ocorreu um erro inesperado ao buscar os posts."
                });
            }
        }
        
        /// <summary>
        /// Lista os alunos verificados associados a um campus específico.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite a recuperação de uma lista de usuários (alunos) que pertencem a um determinado campus
        /// e que tiveram seus e-mails verificados.
        /// </remarks>
        /// <param name="campusId">O ID único do campus para o qual se deseja listar os alunos.</param>
        /// <returns>Uma lista de objetos <see cref="SearchedUserInfo"/> representando os alunos encontrados.</returns>
        /// <response code="200">Retorna a lista de alunos do campus com sucesso.</response>
        /// <response code="400">O ID do campus fornecido é inválido.</response>
        /// <response code="404">Nenhum aluno verificado foi encontrado para o ID de campus especificado.</response>
        /// <response code="500">Ocorreu um erro interno no servidor.</response>
        [HttpGet("buscar/{campusId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<SearchedUserInfo>), 200)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<IEnumerable<SearchedUserInfo>>> GetUserByCampiId([FromRoute] int campusId)
        {
            try
            {
                if (campusId <= 0) 
                {
                    return BadRequest(new ErrorDTO
                    {
                        ErrorCode = "VAL400INVALIDCAMPUSID",
                        Message = "ID do campus inválido.",
                        ErrorDescription = "O ID fornecido para o campus deve ser um número inteiro positivo."
                    });
                }

                var users = await _context.Users
                    .Where(u => u.CdCampus == campusId && u.IsEmailVerified) 
                    .Select(u => new SearchedUserInfo(u)) 
                    .ToListAsync();
                
                if (!users.Any())
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "RES404STUDENTSNOTFOUND",
                        Message = "Nenhum aluno verificado encontrado para este campus.",
                        ErrorDescription =
                            $"Não foram encontrados alunos verificados para o campus com ID: {campusId}."
                    });
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter alunos por campus: {ex.Message}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500LISTSTUDENTSBYCAMPUS",
                    Message = "Não foi possível listar os alunos para este campus.",
                    ErrorDescription = "Ocorreu um erro inesperado ao buscar os alunos."
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