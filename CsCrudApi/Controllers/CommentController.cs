using System.Security.Claims;
using CsCrudApi.DTOs;
using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // A rota base será /api/Comment
    public class CommentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommentController(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// Cria um novo comentário em um post.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite que um usuário autenticado adicione um comentário a um post existente.
        /// </remarks>
        /// <param name="commentary">Objeto contendo os dados do novo comentário (PostGUID e Text).</param>
        /// <returns>
        /// Retorna um status 200 OK com o comentário criado (incluindo timestamps e ID).
        /// Retorna um status 400 Bad Request se campos obrigatórios estiverem vazios ou houver erro de serialização.
        /// Retorna um status 401 Unauthorized se o token JWT for inválido ou ausente.
        /// Retorna um status 404 Not Found se o post de destino não for encontrado.
        /// Retorna um status 500 Internal Server Error em caso de erro inesperado no servidor.
        /// </returns>
        /// <response code="200">Comentário criado com sucesso.</response>
        /// <response code="400">Dados da requisição inválidos (campos vazios, erro de serialização).</response>
        /// <response code="401">Não autorizado (token JWT inválido ou ausente).</response>
        /// <response code="404">Post não encontrado.</response>
        /// <response code="500">Erro interno do servidor.</response>
        [HttpPost]
        [Authorize]
        [RequireHttps]
        [ProducesResponseType(typeof(Commentary), 200)] // Retorna a entidade Commentary
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 401)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<object>> CreateCommentary([FromBody] CommentRequestDTO commentaryRequest)
        {
            if (string.IsNullOrEmpty(commentaryRequest.PostGUID))
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400NULL",
                    Message = "O GUID do post não pode estar vazio.",
                    ErrorDescription = "Verifique se o PostGUID foi enviado na requisição."
                });
            }
            if (string.IsNullOrEmpty(commentaryRequest.Text)) // Adicionada validação de texto
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400NULL",
                    Message = "O texto do comentário não pode estar vazio.",
                    ErrorDescription = "Verifique se o texto do comentário foi enviado na requisição."
                });
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400SERIALIZATION",
                    Message = "Erro na serialização dos dados do comentário.",
                    ErrorDescription = "Verifique o formato do JSON enviado."
                });
            }

            try
            {
                ClaimsPrincipal claimsPrincipal = HttpContext.User;
                var userEmail =  TokenServices.GetTokenEmailAsync(claimsPrincipal);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail); 
                if (user == null)
                {
                    return Unauthorized(new ErrorDTO
                    {
                        ErrorCode = "AUTH401USER",
                        Message = "Token Inválido ou usuário não encontrado.",
                        ErrorDescription = "O usuário associado ao token de autenticação não foi encontrado no sistema."
                    });
                }

                var post = await _context.Posts.FirstOrDefaultAsync(p => p.Guid == commentaryRequest.PostGUID);
                if (post == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "404POST",
                        Message = "Post não encontrado.",
                        ErrorDescription = $"O post com o GUID '{commentaryRequest.PostGUID}' não existe."
                    });
                }

                var commentary = new Commentary
                {
                    PostGUID = commentaryRequest.PostGUID,
                    Text = commentaryRequest.Text,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow,
                    UserId = user.UserId
                };

                _context.Commentaries.Add(commentary);
                await _context.SaveChangesAsync();
                return Ok(commentary);
            }
            catch (Exception ex)
            {
                // Logar ex é crucial
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "500GENERIC",
                    Message = "Ocorreu um erro interno. Tente novamente mais tarde.",
                    ErrorDescription = "Erro inesperado ao criar o comentário."
                });
            }
        }

        /// <summary>
        /// Obtém comentários de um post específico pelo GUID do post.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite buscar todos os comentários associados a um determinado post,
        /// identificado pelo seu GUID. As informações do usuário que fez o comentário também são incluídas.
        /// Este endpoint não requer autenticação.
        /// </remarks>
        /// <param name="guid">O GUID do post para o qual se deseja buscar os comentários.</param>
        /// <returns>
        /// Retorna um status 200 OK com uma lista de comentários detalhados (usuário e comentário).
        /// Retorna um status 400 Bad Request se o GUID não for fornecido.
        /// Retorna um status 404 Not Found se nenhum comentário for encontrado para o GUID do post.
        /// Retorna um status 500 Internal Server Error em caso de erro inesperado no servidor.
        /// </returns>
        /// <response code="200">Lista de comentários encontrados para o post.</response>
        /// <response code="400">GUID do post não fornecido.</response>
        /// <response code="404">Nenhum comentário encontrado para o post.</response>
        /// <response code="500">Erro interno do servidor.</response>
        [AllowAnonymous]
        [HttpGet("{guid}")]
        [ProducesResponseType(typeof(List<CommentDetailsDTO>), 200)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<object>> GetCommentByGUID([FromRoute] string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400NULL",
                    Message = "Por favor, informe um GUID.",
                    ErrorDescription = "O GUID do post é obrigatório para a busca de comentários."
                });
            }

            try
            {
                // O uso de .ToList() antes do ForEach para evitar múltiplas chamadas ao DB por iteração.
                // E a projeção em uma query única (JOIN) é muito mais eficiente.
                var comments = await _context.Commentaries
                                            .Where(c => c.PostGUID == guid)
                                            .OrderByDescending(c => c.CreatedAt)
                                            .ToListAsync();

                if (!comments.Any()) 
                {
                    return NoContent();
                }
                
                // TODO: Melhorar com propriedades de Navegação
                List<CommentDetailsDTO> result = new List<CommentDetailsDTO>();
                foreach (var comment in comments)
                {
                    var user = await _context.Users
                        .Select(u => new SearchedUserInfo(
                        u))
                        .FirstOrDefaultAsync(u => u.UserId == comment.UserId);

                    result.Add(new CommentDetailsDTO
                    {
                        User = user,
                        Comment = comment
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "500GENERIC",
                    Message = "Ocorreu um erro interno. Tente novamente mais tarde.",
                    ErrorDescription = "Erro inesperado ao buscar comentários."
                });
            }
        }

        /// <summary>
        /// Atualiza um comentário existente.
        /// </summary>
        /// <remarks>
        /// Permite que o autor de um comentário atualize seu texto.
        /// O token JWT do usuário autenticado é validado para garantir que apenas o proprietário pode editar.
        /// O comentário deve ter pelo menos 3 caracteres de texto.
        /// </remarks>
        /// <param name="updatedCommentary">Objeto com o ID do comentário a ser atualizado, o GUID do post e o novo texto.</param>
        /// <returns>
        /// Retorna um status 204 No Content sem conteúdo.
        /// Retorna um status 400 Bad Request se os dados da requisição forem inválidos ou o texto for muito curto.
        /// Retorna um status 401 Unauthorized se o token JWT for inválido ou ausente.
        /// Retorna um status 403 Forbidden se o usuário autenticado não for o autor do comentário.
        /// Retorna um status 404 Not Found se o post ou o comentário não forem encontrados.
        /// Retorna um status 500 Internal Server Error em caso de erro inesperado no servidor.
        /// </returns>
        /// <response code="200">Comentário atualizado com sucesso.</response>
        /// <response code="400">Dados da requisição inválidos (campos vazios, serialização, texto curto).</response>
        /// <response code="401">Não autorizado (token JWT inválido ou ausente).</response>
        /// <response code="403">Proibido (usuário não é o autor do comentário).</response>
        /// <response code="404">Post ou comentário não encontrado.</response>
        /// <response code="500">Erro interno do servidor.</response>
        [HttpPut]
        [Authorize]
        [RequireHttps]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 401)]
        [ProducesResponseType(typeof(ErrorDTO), 403)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<object>> UpdateCommentary([FromBody] CommentRequestDTO updatedCommentaryRequest)
        {
            // Validações usando DTO e data annotations (se aplicável)
            if (string.IsNullOrEmpty(updatedCommentaryRequest.PostGUID))
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400NULL",
                    Message = "Verifique se o GUID do post foi preenchido.",
                    ErrorDescription = "O PostGUID é obrigatório para a atualização do comentário."
                });
            }

            if (string.IsNullOrEmpty(updatedCommentaryRequest.Text) || updatedCommentaryRequest.Text.Length < 3)
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400TEXTLENGTH",
                    Message = "O comentário precisa ter texto igual ou superior a 3 letras.",
                    ErrorDescription = "O campo 'Text' do comentário é obrigatório e deve ter no mínimo 3 caracteres."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400SERIALIZATION",
                    Message = "Erro na serialização dos dados do comentário.",
                    ErrorDescription = "Verifique o formato do JSON enviado."
                });
            }

            try
            {
                ClaimsPrincipal claimsPrincipal = HttpContext.User;
                var userEmail =  TokenServices.GetTokenEmailAsync(claimsPrincipal);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    return Unauthorized(new ErrorDTO
                    {
                        ErrorCode = "AUTH401USER",
                        Message = "Token Inválido ou usuário não encontrado.",
                        ErrorDescription = "O usuário associado ao token de autenticação não foi encontrado no sistema."
                    });
                }

                if (!await _context.Posts.AnyAsync(p => p.Guid == updatedCommentaryRequest.PostGUID))
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "404POST",
                        Message = "Post não encontrado.",
                        ErrorDescription = $"O post com o GUID '{updatedCommentaryRequest.PostGUID}' não existe."
                    });
                }

                var savedCommentary = await _context.Commentaries.FirstOrDefaultAsync(sc => sc.Id == updatedCommentaryRequest.Id);

                if (savedCommentary == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "404COMMENT",
                        Message = "Comentário não encontrado.",
                        ErrorDescription = $"O comentário com ID '{updatedCommentaryRequest.Id}' não existe."
                    });
                }

                if (savedCommentary.UserId != user.UserId)
                {
                    return StatusCode(403, new ErrorDTO 
                    {
                        ErrorCode = "AUTH403FORBIDDEN",
                        Message = "Ação não permitida.",
                        ErrorDescription = "Você não tem permissão para editar este comentário, pois não é o autor."
                    });
                }

                savedCommentary.Text = updatedCommentaryRequest.Text;
                savedCommentary.LastUpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(savedCommentary);
            }
            catch (Exception ex)
            {
                // Logar ex é crucial
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "500GENERIC",
                    Message = "Ocorreu um erro interno. Tente novamente mais tarde.",
                    ErrorDescription = "Erro inesperado ao atualizar o comentário."
                });
            }
        }

        /// <summary>
        /// Exclui um comentário existente.
        /// </summary>
        /// <remarks>
        /// Permite que o autor de um comentário o exclua.
        /// O token JWT do usuário autenticado é validado para garantir que apenas o proprietário pode excluir.
        /// </remarks>
        /// <param name="commentaryId">O ID do comentário a ser excluído.</param>
        /// <returns>
        /// Retorna um status 200 OK em caso de exclusão bem-sucedida.
        /// Retorna um status 400 Bad Request se o ID do comentário for inválido.
        /// Retorna um status 401 Unauthorized se o token JWT for inválido ou ausente.
        /// Retorna um status 403 Forbidden se o usuário autenticado não for o autor do comentário.
        /// Retorna um status 404 Not Found se o comentário não for encontrado.
        /// Retorna um status 500 Internal Server Error em caso de erro inesperado no servidor.
        /// </returns>
        /// <response code="204">Comentário excluído com sucesso.</response>
        /// <response code="400">ID do comentário inválido.</response>
        /// <response code="401">Não autorizado (token JWT inválido ou ausente).</response>
        /// <response code="403">Proibido (usuário não é o autor do comentário).</response>
        /// <response code="404">Comentário não encontrado.</response>
        /// <response code="500">Erro interno do servidor.</response>
        [HttpDelete]
        [Authorize]
        [RequireHttps]
        [ProducesResponseType(204)] 
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 401)]
        [ProducesResponseType(typeof(ErrorDTO), 403)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult> DeleteCommentary([FromQuery] int commentaryId)
        {
            // Validações
            if (commentaryId == 0)
            {
                return BadRequest(new ErrorDTO
                {
                    ErrorCode = "BR400ID",
                    Message = "O ID do comentário precisa estar preenchido.",
                    ErrorDescription = "O parâmetro 'commentaryId' na query string é obrigatório e deve ser maior que zero."
                });
            }

            try
            {
                ClaimsPrincipal claimsPrincipal = HttpContext.User;
                var userEmail =  TokenServices.GetTokenEmailAsync(claimsPrincipal);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    return Unauthorized(new ErrorDTO
                    {
                        ErrorCode = "AUTH401USER",
                        Message = "Token Inválido ou usuário não encontrado.",
                        ErrorDescription = "O usuário associado ao token de autenticação não foi encontrado no sistema."
                    });
                }

                var commentary = await _context.Commentaries.FirstOrDefaultAsync(c => c.Id == commentaryId);
                if (commentary == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "404COMMENT",
                        Message = "Comentário não encontrado.",
                        ErrorDescription = $"O comentário com ID '{commentaryId}' não existe."
                    });
                }

                if (commentary.UserId != user.UserId)
                {
                    return StatusCode(403, new ErrorDTO
                    {
                        ErrorCode = "AUTH403FORBIDDEN",
                        Message = "Ação não permitida.",
                        ErrorDescription = "Você não tem permissão para excluir este comentário, pois não é o autor."
                    });
                }

                _context.Commentaries.Remove(commentary);
                await _context.SaveChangesAsync();
                return NoContent(); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "500GENERIC",
                    Message = "Ocorreu um erro interno. Tente novamente mais tarde.",
                    ErrorDescription = "Erro inesperado ao excluir o comentário."
                });
            }
        }
    }
}